using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(32, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9_.-]+$", ErrorMessage = "Use letters, numbers, dots, dashes and underscores only.")]
    public string Username { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
