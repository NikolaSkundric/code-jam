using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// User-to-user ban. Unique index on (BannerId, BannedUserId) filtered by IsDeleted = false.
/// Terminates the friendship and freezes the personal chat history (read-only).
/// </summary>
public class UserBan : SoftDeletableEntity
{
    public Guid BannerId { get; set; }
    public User Banner { get; set; } = null!;

    public Guid BannedUserId { get; set; }
    public User BannedUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
