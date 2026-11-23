using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// One-to-one dialog: a chat with a fixed participant list of two users.
/// Stored with UserAId &lt; UserBId (unique pair). Id equals Chat.Id (shared primary key).
/// Soft deleted (history is preserved, e.g. frozen after a user ban).
/// </summary>
public class PersonalChat : SoftDeletableEntity
{
    public Chat Chat { get; set; } = null!;

    public Guid UserAId { get; set; }
    public User UserA { get; set; } = null!;

    public Guid UserBId { get; set; }
    public User UserB { get; set; } = null!;
}
