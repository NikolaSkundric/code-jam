namespace CodingJamAPI.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;

    /// <summary>HMAC key (at least 32 characters). When it is not configured a random key is generated at startup.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string CookieName { get; init; } = "access_token";

    /// <summary>Lifetime of a normal session (cookie without Expires, i.e. dropped when the browser closes).</summary>
    public int SessionHours { get; init; } = 24;

    /// <summary>Lifetime of a "Keep me signed in" session (persistent cookie).</summary>
    public int RememberMeDays { get; init; } = 30;
}
