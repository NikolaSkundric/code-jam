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

public sealed class FriendService(AppDbContext db, IRealtimeNotifier notifier, IBanService bans, IJabberGateway jabber) : IFriendService
{
    // ---------- friends ----------

    public async Task<IReadOnlyList<FriendResponse>> GetFriendsAsync(Guid userId, CancellationToken ct)
    {
        var rows = await db.Friendships.AsNoTracking()
            .Where(f => f.UserAId == userId || f.UserBId == userId)
            .Select(f => new { FriendId = f.UserAId == userId ? f.UserBId : f.UserAId, f.CreatedAt })
            .ToListAsync(ct);

        var names = await LoadUsernamesAsync(rows.Select(r => r.FriendId), ct);
        return rows
            .Where(r => names.ContainsKey(r.FriendId))
            .Select(r => new FriendResponse(r.FriendId, names[r.FriendId], r.CreatedAt))
            .OrderBy(f => f.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken ct)
    {
        var (a, b) = GuidPair.Order(userId, friendId);
        var friendship = await db.Friendships.FirstOrDefaultAsync(f => f.UserAId == a && f.UserBId == b, ct)
                         ?? throw new NotFoundException("Friend not found.");

        db.Friendships.Remove(friendship); // soft delete
        await db.SaveChangesAsync(ct);
        await notifier.FriendsChangedAsync([userId, friendId], ct);
        await SyncRosterAsync(userId, friendId, add: false, ct);
    }

    // ---------- requests ----------

    public async Task<FriendRequestsResponse> GetRequestsAsync(Guid userId, CancellationToken ct)
    {
        var pending = await db.FriendRequests.AsNoTracking()
            .Where(r => r.Status == RequestStatus.Pending && (r.SenderId == userId || r.RecipientId == userId))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var names = await LoadUsernamesAsync(pending.SelectMany(r => new[] { r.SenderId, r.RecipientId }), ct);

        FriendRequestResponse? Map(FriendRequest r, Guid otherId) =>
            names.TryGetValue(otherId, out var name)
                ? new FriendRequestResponse(r.Id, otherId, name, r.Message, r.CreatedAt)
                : null;

        var incoming = pending.Where(r => r.RecipientId == userId)
            .Select(r => Map(r, r.SenderId)).OfType<FriendRequestResponse>().ToList();
        var outgoing = pending.Where(r => r.SenderId == userId)
            .Select(r => Map(r, r.RecipientId)).OfType<FriendRequestResponse>().ToList();
        return new FriendRequestsResponse(incoming, outgoing);
    }

    public async Task SendRequestAsync(Guid userId, SendFriendRequestRequest request, CancellationToken ct)
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
            throw new ValidationFailedException("username", "You cannot add yourself as a friend.");
        }

        // A ban in either direction blocks any contact; do not reveal who banned whom.
        if (await bans.IsBlockedAsync(userId, target.Id, ct))
        {
            throw new ValidationFailedException("username", "You cannot send a friend request to this user.");
        }

        var (a, b2) = GuidPair.Order(userId, target.Id);
        if (await db.Friendships.AnyAsync(f => f.UserAId == a && f.UserBId == b2, ct))
        {
            throw new ValidationFailedException("username", "You are already friends.");
        }

        var existing = await db.FriendRequests.Where(r =>
                r.Status == RequestStatus.Pending &&
                ((r.SenderId == userId && r.RecipientId == target.Id) ||
                 (r.SenderId == target.Id && r.RecipientId == userId)))
            .Select(r => r.SenderId)
            .FirstOrDefaultAsync(ct);
        if (existing != Guid.Empty)
        {
            throw new ValidationFailedException("username", existing == userId
                ? "You already sent a friend request to this user."
                : "This user already sent you a request. Accept it in your incoming requests.");
        }

        var message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
        db.FriendRequests.Add(new FriendRequest
        {
            SenderId = userId,
            RecipientId = target.Id,
            Message = message,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ValidationFailedException("username", "You already sent a friend request to this user.");
        }

        await notifier.FriendsChangedAsync([userId, target.Id], ct);
    }

    public async Task AcceptRequestAsync(Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await FindPendingAsync(r => r.Id == requestId && r.RecipientId == userId, ct);
        if (await bans.IsBlockedAsync(request.SenderId, request.RecipientId, ct))
        {
            throw new NotFoundException("Friend request not found.");
        }

        var (a, b) = GuidPair.Order(request.SenderId, request.RecipientId);
        var now = DateTime.UtcNow;

        request.Status = RequestStatus.Accepted;
        request.RespondedAt = now;

        if (!await db.Friendships.AnyAsync(f => f.UserAId == a && f.UserBId == b, ct))
        {
            db.Friendships.Add(new Friendship { UserAId = a, UserBId = b, CreatedAt = now });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The friendship was created concurrently; the request is still accepted.
            db.ChangeTracker.Clear();
            await db.FriendRequests.Where(r => r.Id == requestId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RequestStatus.Accepted)
                    .SetProperty(r => r.RespondedAt, now), ct);
        }

        await notifier.FriendsChangedAsync([request.SenderId, request.RecipientId], ct);
        await SyncRosterAsync(request.SenderId, request.RecipientId, add: true, ct);
    }

    public async Task DeclineRequestAsync(Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await FindPendingAsync(r => r.Id == requestId && r.RecipientId == userId, ct);
        request.Status = RequestStatus.Declined;
        request.RespondedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await notifier.FriendsChangedAsync([request.SenderId, request.RecipientId], ct);
    }

    public async Task CancelRequestAsync(Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await FindPendingAsync(r => r.Id == requestId && r.SenderId == userId, ct);
        db.FriendRequests.Remove(request); // soft delete
        await db.SaveChangesAsync(ct);
        await notifier.FriendsChangedAsync([request.SenderId, request.RecipientId], ct);
    }

    // ---------- helpers ----------

    /// <summary>Friends see each other in their Jabber rosters (best effort, not awaited).</summary>
    private async Task SyncRosterAsync(Guid userA, Guid userB, bool add, CancellationToken ct)
    {
        var names = await LoadUsernamesAsync([userA, userB], ct);
        if (names.TryGetValue(userA, out var nameA) && names.TryGetValue(userB, out var nameB))
        {
            _ = add ? jabber.AddFriendshipAsync(nameA, nameB) : jabber.RemoveFriendshipAsync(nameA, nameB);
        }
    }

    private async Task<FriendRequest> FindPendingAsync(
        System.Linq.Expressions.Expression<Func<FriendRequest, bool>> predicate, CancellationToken ct) =>
        await db.FriendRequests.Where(r => r.Status == RequestStatus.Pending).Where(predicate).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Friend request not found.");

    private async Task<Dictionary<Guid, string>> LoadUsernamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var distinct = ids.Distinct().ToList();
        return await db.Users.AsNoTracking()
            .Where(u => distinct.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);
    }
}
