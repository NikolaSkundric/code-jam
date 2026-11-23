namespace CodingJamAPI.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public string CookieName { get; init; } = "access_token";

    /// <summary>Lifetime of a normal session (cookie without Expires, i.e. dropped when the browser closes).</summary>
    public int SessionHours { get; init; } = 24;

    /// <summary>Lifetime of a "Keep me signed in" session (persistent cookie).</summary>
    public int RememberMeDays { get; init; } = 30;
}
