namespace CodingJamAPI.Application.Models;

/// <summary>A file received with a message, independent of the web framework.</summary>
public sealed record UploadedFile(string FileName, string ContentType, long Length, Func<Stream> Open, string? Comment);
