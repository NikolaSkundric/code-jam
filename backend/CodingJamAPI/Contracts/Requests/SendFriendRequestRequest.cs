using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class SendFriendRequestRequest
{
    [Required, StringLength(32, MinimumLength = 1)]
    public string Username { get; init; } = string.Empty;

    /// <summary>Optional text shown to the recipient.</summary>
    [MaxLength(500)]
    public string? Message { get; init; }
}
