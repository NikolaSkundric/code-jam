namespace CodingJamAPI.Application.Abstractions;

/// <summary>
/// Decides whose presence a user may see (TASK.md 2.2.1: presence of contacts): friends and people
/// who share a room with them. The relation is symmetric, so the same set is also the audience that
/// is told when this user changes status.
/// </summary>
public interface IPresenceAudienceService
{
    Task<HashSet<Guid>> GetVisibleUsersAsync(Guid userId, CancellationToken ct);
}
