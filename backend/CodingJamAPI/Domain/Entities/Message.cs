using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Deleting a message (by author or room admin) is a soft delete; the UI hides deleted messages.
/// Messages are hard deleted only when their room is deleted. Ordering and infinite scroll use
/// the (CreatedAt, Id) pair as cursor.
/// </summary>
public class Message : SoftDeletableEntity
{
    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    /// <summary>Authors are soft deleted, so the reference always stays valid.</summary>
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    /// <summary>UTF-8 text, max 3 KB (enforced in validation).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Optional reply/reference to another message.</summary>
    public Guid? ReplyToMessageId { get; set; }
    public Message? ReplyToMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Set when edited; UI shows a gray edited indicator.</summary>
    public DateTime? EditedAt { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
