using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;
}
