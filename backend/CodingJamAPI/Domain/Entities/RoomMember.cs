using CodingJamAPI.Domain.Common;
using CodingJamAPI.Domain.Enums;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Unique index on (RoomId, UserId) filtered by IsDeleted = false. Leaving or being removed
/// soft deletes the row. The owner always has an active row with Role = Owner.
/// </summary>
public class RoomMember : SoftDeletableEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public RoomRole Role { get; set; } = RoomRole.Member;
    public DateTime JoinedAt { get; set; }
}
