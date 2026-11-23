using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class EditMessageRequest
{
    [Required, MaxLength(6000)]
    public string Text { get; init; } = string.Empty;
}
