using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>Single use: consumed by setting UsedAt, never deleted.</summary>
public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Hash of the single-use token (the raw token is only sent to the user).</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
