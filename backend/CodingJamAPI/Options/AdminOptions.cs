namespace CodingJamAPI.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>Usernames (case-insensitive) with access to the administration pages.</summary>
    public string[] Usernames { get; init; } = [];

    public bool IsAdmin(string username) =>
        Usernames.Any(u => string.Equals(u.Trim(), username, StringComparison.OrdinalIgnoreCase));
}
