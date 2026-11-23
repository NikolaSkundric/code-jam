namespace CodingJamAPI.Options;

public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Public URL of the frontend, used to build links in emails.</summary>
    public string PublicBaseUrl { get; init; } = "http://localhost:3000";
}
