using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class ResetPasswordRequest
{
    [Required, MaxLength(256)]
    public string Token { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; init; } = string.Empty;
}
