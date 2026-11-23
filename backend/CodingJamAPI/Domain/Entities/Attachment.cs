using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Access is checked through Message.ChatId (current room member / personal chat participant).
/// Soft deleted together with its message; the file on disk is removed only when the room
/// is deleted (permanent deletion required by TASK.md).
/// </summary>
public class Attachment : SoftDeletableEntity
{
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;

    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;

    /// <summary>Original file name, preserved for download.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Relative path/key in local file storage (never user-controlled).</summary>
    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    /// <summary>Max 20 MB for files, 3 MB for images.</summary>
    public long SizeBytes { get; set; }
    public bool IsImage { get; set; }

    /// <summary>Optional comment added by the uploader.</summary>
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
