using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Unique index on (RoomId, UserId) filtered by IsDeleted = false. Created when an admin bans or
/// removes a member; the user cannot rejoin until the ban is removed (soft deleted).
/// </summary>
public class RoomBan : SoftDeletableEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>The admin who issued the ban (visible to admins).</summary>
    public Guid BannedById { get; set; }
    public User BannedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
