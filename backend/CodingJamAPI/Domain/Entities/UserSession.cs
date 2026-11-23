using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>One row per signed-in browser; Id is the JWT jti claim. Logout is soft: RevokedAt is set.</summary>
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the session is logged out/revoked (the row is kept).</summary>
    public DateTime? RevokedAt { get; set; }
}
