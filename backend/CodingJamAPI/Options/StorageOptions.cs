namespace CodingJamAPI.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Directory for uploaded files (a Docker volume in compose). Relative paths are resolved against the content root.</summary>
    public string RootPath { get; init; } = "uploads";

    /// <summary>TASK.md 3.4: maximum file size.</summary>
    public long MaxFileBytes { get; init; } = 20 * 1024 * 1024;

    /// <summary>TASK.md 3.4: maximum image size.</summary>
    public long MaxImageBytes { get; init; } = 3 * 1024 * 1024;

    public int MaxFilesPerMessage { get; init; } = 5;
}
