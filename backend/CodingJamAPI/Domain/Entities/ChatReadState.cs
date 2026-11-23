using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Per-user read marker; unique index on (ChatId, UserId). Unread = messages in the chat with
/// CreatedAt greater than LastReadAt; cleared when the user opens the chat. Never deleted on its own
/// (removed with the chat).
/// </summary>
public class ChatReadState : BaseEntity
{
    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime LastReadAt { get; set; }
}
