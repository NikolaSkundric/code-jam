using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Confirmed, symmetric friendship. Stored once per pair with UserAId &lt; UserBId.
/// Unique index on (UserAId, UserBId) filtered by IsDeleted = false. Removing a friend or a
/// user ban soft deletes the row.
/// </summary>
public class Friendship : SoftDeletableEntity
{
    public Guid UserAId { get; set; }
    public User UserA { get; set; } = null!;

    public Guid UserBId { get; set; }
    public User UserB { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
