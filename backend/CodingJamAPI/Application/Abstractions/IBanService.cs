using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;

namespace CodingJamAPI.Application.Abstractions;

/// <summary>User-to-user bans (TASK.md 2.3.5).</summary>
public interface IBanService
{
    /// <summary>
    /// True if either user has banned the other. While true, the two users cannot contact each other:
    /// no friend requests and no new personal messages; their existing personal chat is read-only.
    /// Messaging code must also require a friendship (TASK.md 2.3.6).
    /// </summary>
    Task<bool> IsBlockedAsync(Guid userA, Guid userB, CancellationToken ct);

    Task<IReadOnlyList<BannedUserResponse>> GetBannedAsync(Guid userId, CancellationToken ct);

    /// <summary>Bans a user: ends the friendship and cancels pending friend requests between the two.</summary>
    Task BanAsync(Guid userId, BanUserRequest request, CancellationToken ct);

    Task UnbanAsync(Guid userId, Guid bannedUserId, CancellationToken ct);
}
