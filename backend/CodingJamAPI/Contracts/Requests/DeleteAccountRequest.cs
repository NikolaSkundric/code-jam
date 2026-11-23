using System.ComponentModel.DataAnnotations;

namespace CodingJamAPI.Contracts.Requests;

public sealed class DeleteAccountRequest
{
    /// <summary>Current password, required to confirm account removal.</summary>
    [Required, MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
