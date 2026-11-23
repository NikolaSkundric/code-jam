using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class BanUserRequest
{
    [Required, StringLength(32, MinimumLength = 1)]
    public string Username { get; init; } = string.Empty;
}
