using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class OpenPersonalChatRequest
{
    [Required]
    public Guid UserId { get; init; }
}
