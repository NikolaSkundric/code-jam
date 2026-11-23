using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

/// <summary>multipart/form-data variant of SendMessageRequest with attachments.</summary>
public sealed class SendMessageFormRequest
{
    [MaxLength(6000)]
    public string? Text { get; init; }

    public Guid? ReplyToMessageId { get; init; }

    /// <summary>Files to attach (field name "files").</summary>
    public List<IFormFile> Files { get; init; } = [];

    /// <summary>Optional comment per file, same order as Files (field name "comments").</summary>
    public List<string?> Comments { get; init; } = [];
}
