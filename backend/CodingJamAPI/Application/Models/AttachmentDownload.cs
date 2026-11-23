namespace CodingJamAPI.Application.Models;

public sealed record AttachmentDownload(Stream Content, string FileName, string ContentType, bool IsImage, long SizeBytes);
