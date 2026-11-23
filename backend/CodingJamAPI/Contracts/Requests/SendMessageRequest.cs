using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class SendMessageRequest
{
    /// <summary>UTF-8 text; the 3 KB limit is checked in bytes by the service. May be empty when files are attached.</summary>
    [MaxLength(6000)]
    public string? Text { get; init; }

    public Guid? ReplyToMessageId { get; init; }
}
