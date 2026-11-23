using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required, MaxLength(128)]
    public string Password { get; init; } = string.Empty;

    public bool KeepSignedIn { get; init; }
}
