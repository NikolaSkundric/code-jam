using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Domain.Common;
using CodingJamAPI.Domain.Entities;
using CodingJamAPI.Domain.Enums;
using CodingJamAPI.Domain.Exceptions;
using CodingJamAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CodingJamAPI.Application.Services;

public sealed class BanService(AppDbContext db, IRealtimeNotifier notifier, IJabberGateway jabber) : IBanService
{
    public Task<bool> IsBlockedAsync(Guid userA, Guid userB, CancellationToken ct) =>
        db.UserBans.AnyAsync(b =>
            (b.BannerId == userA && b.BannedUserId == userB) ||
            (b.BannerId == userB && b.BannedUserId == userA), ct);

    public async Task<IReadOnlyList<BannedUserResponse>> GetBannedAsync(Guid userId, CancellationToken ct)
    {
        var bans = await db.UserBans.AsNoTracking()
            .Where(b => b.BannerId == userId)
            .Select(b => new { b.BannedUserId, b.CreatedAt })
            .ToListAsync(ct);

        var ids = bans.Select(b => b.BannedUserId).ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);

        return bans
            .Where(b => names.ContainsKey(b.BannedUserId))
            .Select(b => new BannedUserResponse(b.BannedUserId, names[b.BannedUserId], b.CreatedAt))
            .OrderBy(b => b.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task BanAsync(Guid userId, BanUserRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var target = await db.Users.AsNoTracking()
            .Where(u => u.Username.ToLower() == username)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(ct);

        if (target is null)
        {
            throw new ValidationFailedException("username", "No user with this username.");
        }
        if (target.Id == userId)
        {
            throw new ValidationFailedException("username", "You cannot ban yourself.");
        }
        if (await db.UserBans.AnyAsync(b => b.BannerId == userId && b.BannedUserId == target.Id, ct))
        {
            throw new ValidationFailedException("username", "You have already banned this user.");
        }

        db.UserBans.Add(new UserBan { BannerId = userId, BannedUserId = target.Id, CreatedAt = DateTime.UtcNow });

        // The friend relationship is terminated (soft delete).
        var (a, b2) = GuidPair.Order(userId, target.Id);
        var friendships = await db.Friendships.Where(f => f.UserAId == a && f.UserBId == b2).ToListAsync(ct);
        db.Friendships.RemoveRange(friendships);

        // Pending friend requests between the two users are withdrawn (soft delete).
        var requests = await db.FriendRequests
            .Where(r => r.Status == RequestStatus.Pending &&
                        ((r.SenderId == userId && r.RecipientId == target.Id) ||
                         (r.SenderId == target.Id && r.RecipientId == userId)))
            .ToListAsync(ct);
        db.FriendRequests.RemoveRange(requests);

        try
        {
            await db.SaveChangesAsync(ct); // one transaction: ban + friendship + requests
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ValidationFailedException("username", "You have already banned this user.");
        }

        await notifier.FriendsChangedAsync([userId, target.Id], ct);

        if (friendships.Count > 0)
        {
            var names = await db.Users.AsNoTracking()
                .Where(u => u.Id == userId || u.Id == target.Id)
                .ToDictionaryAsync(u => u.Id, u => u.Username, ct);
            if (names.TryGetValue(userId, out var banner) && names.TryGetValue(target.Id, out var banned))
            {
                _ = jabber.RemoveFriendshipAsync(banner, banned); // best effort, not awaited
            }
        }
    }

    public async Task UnbanAsync(Guid userId, Guid bannedUserId, CancellationToken ct)
    {
        var ban = await db.UserBans.FirstOrDefaultAsync(b => b.BannerId == userId && b.BannedUserId == bannedUserId, ct)
                  ?? throw new NotFoundException("Ban not found.");

        db.UserBans.Remove(ban); // soft delete
        await db.SaveChangesAsync(ct);
        await notifier.FriendsChangedAsync([userId, bannedUserId], ct);
    }
}
