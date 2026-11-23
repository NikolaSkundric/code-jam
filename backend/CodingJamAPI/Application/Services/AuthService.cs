using System.Security.Cryptography;
using System.Text;
using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Domain.Entities;
using CodingJamAPI.Domain.Exceptions;
using CodingJamAPI.Infrastructure.Persistence;
using CodingJamAPI.Infrastructure.Realtime;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CodingJamAPI.Application.Services;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher hasher,
    ITokenService tokens,
    IEmailSender emailSender,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AppOptions> appOptions,
    IFileStorage storage,
    IRealtimeNotifier notifier) : IAuthService
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);
    private const string InvalidCredentials = "Invalid email or password.";

    private readonly JwtOptions _jwt = jwtOptions.Value;

    // ---------- registration / sign in / sign out ----------

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, ClientInfo client, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var username = request.Username.Trim();
        var lowerUsername = username.ToLowerInvariant();

        var errors = new Dictionary<string, string[]>();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            errors["email"] = new[] { "Email is already registered." };
        }
        if (await db.Users.AnyAsync(u => u.Username.ToLower() == lowerUsername, ct))
        {
            errors["username"] = new[] { "Username is already taken." };
        }
        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        var user = new User
        {
            Email = email,
            Username = username,
            PasswordHash = hasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Lost a race with a concurrent registration.
            throw new ValidationFailedException("email", "Email or username is already taken.");
        }

        return await StartSessionAsync(user, keepSignedIn: false, client, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, ClientInfo client, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Always run a hash verification so response time does not reveal whether the email exists.
        var passwordOk = hasher.Verify(request.Password, user?.PasswordHash);
        if (user is null || !passwordOk)
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        return await StartSessionAsync(user, request.KeepSignedIn, client, ct);
    }

    public async Task LogoutAsync(Guid sessionId, CancellationToken ct)
    {
        await RevokeSessionsAsync(s => s.Id == sessionId, ct);
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedException("Not signed in.");
        return ToResponse(user);
    }

    // ---------- password management ----------

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            return; // Never reveal whether the email exists.
        }

        var now = DateTime.UtcNow;

        // Invalidate earlier unused tokens for this user.
        await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);

        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = now,
            ExpiresAt = now.Add(ResetTokenLifetime)
        });
        await db.SaveChangesAsync(ct);

        var baseUrl = appOptions.Value.PublicBaseUrl.TrimEnd('/');
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        await emailSender.SendPasswordResetAsync(user.Email, link, ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var tokenHash = HashToken(request.Token);
        var now = DateTime.UtcNow;

        var token = await db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > now, ct);
        var user = token is null ? null : await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (token is null || user is null)
        {
            throw new ValidationFailedException("token", "This reset link is invalid or has expired.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        user.PasswordHash = hasher.Hash(request.NewPassword);
        token.UsedAt = now;
        await db.SaveChangesAsync(ct);
        await RevokeSessionsAsync(s => s.UserId == user.Id, ct); // sign out everywhere
        await tx.CommitAsync(ct);
    }

    public async Task ChangePasswordAsync(Guid userId, Guid currentSessionId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedException("Not signed in.");

        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationFailedException("currentPassword", "Current password is incorrect.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        user.PasswordHash = hasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);
        // Keep this browser signed in, drop every other session.
        await RevokeSessionsAsync(s => s.UserId == userId && s.Id != currentSessionId, ct);
        await tx.CommitAsync(ct);
    }

    // ---------- active sessions ----------

    public async Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(Guid userId, Guid currentSessionId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await db.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastSeenAt)
            .Select(s => new SessionResponse(
                s.Id, s.UserAgent, s.IpAddress, s.CreatedAt, s.LastSeenAt, s.Id == currentSessionId))
            .ToListAsync(ct);
    }

    public async Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var revoked = await RevokeSessionsAsync(s => s.UserId == userId && s.Id == sessionId, ct);
        if (revoked == 0)
        {
            throw new NotFoundException("Session not found.");
        }
    }

    // ---------- account removal ----------

    public async Task DeleteAccountAsync(Guid userId, DeleteAccountRequest request, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedException("Not signed in.");

        if (!hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ValidationFailedException("password", "Password is incorrect.");
        }

        var now = DateTime.UtcNow;

        // Collect who must be told (and which upload folders to remove) before the data is gone.
        var ownedRoomIds = await db.Rooms.AsNoTracking().Where(r => r.OwnerId == userId).Select(r => r.Id).ToListAsync(ct);
        var roomMemberIds = ownedRoomIds.Count == 0
            ? []
            : await db.RoomMembers.AsNoTracking()
                .Where(m => ownedRoomIds.Contains(m.RoomId) && m.UserId != userId)
                .Select(m => m.UserId).Distinct().ToListAsync(ct);
        var friendIds = await db.Friendships.AsNoTracking()
            .Where(f => f.UserAId == userId || f.UserBId == userId)
            .Select(f => f.UserAId == userId ? f.UserBId : f.UserAId)
            .ToListAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // TASK.md 2.1.5: rooms owned by the user are deleted permanently with all their
        // messages and attachments (database cascade from the Chat row).
        await db.Chats
            .Where(c => db.Rooms.Any(r => r.Id == c.Id && r.OwnerId == userId))
            .ExecuteDeleteAsync(ct);

        // Membership in other rooms is removed (soft delete).
        await db.RoomMembers.Where(m => m.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsDeleted, true).SetProperty(m => m.DeletedAt, now), ct);

        // Contacts and personal dialogs of a removed account go away too (soft delete).
        await db.Friendships.Where(f => f.UserAId == userId || f.UserBId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.IsDeleted, true).SetProperty(f => f.DeletedAt, now), ct);
        await db.FriendRequests.Where(f => f.SenderId == userId || f.RecipientId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.IsDeleted, true).SetProperty(f => f.DeletedAt, now), ct);
        await db.PersonalChats.Where(p => p.UserAId == userId || p.UserBId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true).SetProperty(p => p.DeletedAt, now), ct);

        await RevokeSessionsAsync(s => s.UserId == userId, ct);

        // Soft delete the account; anonymize the email so it can be registered again.
        var anonymizedEmail = $"deleted-{userId:N}@deleted.invalid";
        await db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.IsDeleted, true)
                .SetProperty(u => u.DeletedAt, now)
                .SetProperty(u => u.Email, anonymizedEmail), ct);

        await tx.CommitAsync(ct);

        // Files of the deleted rooms are removed permanently too (TASK.md 2.1.5, 2.4.6).
        foreach (var roomId in ownedRoomIds)
        {
            storage.DeleteFolder(roomId.ToString("N"));
            await notifier.NotifyAsync(roomMemberIds, RealtimeEvents.RoomsChanged, roomId, ct);
        }
        await notifier.FriendsChangedAsync(friendIds, ct);
    }

    // ---------- helpers ----------

    private async Task<AuthResult> StartSessionAsync(User user, bool keepSignedIn, ClientInfo client, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var expiresAt = keepSignedIn ? now.AddDays(_jwt.RememberMeDays) : now.AddHours(_jwt.SessionHours);

        var session = new UserSession
        {
            UserId = user.Id,
            UserAgent = Truncate(client.UserAgent, 512),
            IpAddress = Truncate(client.IpAddress, 64),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = expiresAt
        };
        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);

        var token = tokens.CreateToken(user.Id, session.Id, expiresAt);
        return new AuthResult(ToResponse(user), token, expiresAt, keepSignedIn);
    }

    private Task<int> RevokeSessionsAsync(System.Linq.Expressions.Expression<Func<UserSession, bool>> predicate, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return db.UserSessions
            .Where(predicate)
            .Where(s => s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), ct);
    }

    private static UserResponse ToResponse(User user) => new(user.Id, user.Email, user.Username);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
