namespace CodingJamAPI.Contracts.Responses;

/// <summary>The message a reply refers to; Text is null when that message was deleted.</summary>
public sealed record ReplyPreviewResponse(Guid Id, string AuthorUsername, string? Text, bool Deleted);

/// <summary>Download via GET /api/attachments/{id} (requires access to the chat).</summary>
public sealed record AttachmentResponse(Guid Id, string FileName, string ContentType, long SizeBytes, bool IsImage, string? Comment);

public sealed record MessageResponse(
    Guid Id,
    Guid ChatId,
    Guid AuthorId,
    string AuthorUsername,
    string Text,
    ReplyPreviewResponse? ReplyTo,
    DateTime CreatedAt,
    DateTime? EditedAt,
    IReadOnlyList<AttachmentResponse> Attachments);

/// <summary>Messages in chronological order (oldest first). HasMore: older messages exist.</summary>
public sealed record MessagePageResponse(IReadOnlyList<MessageResponse> Messages, bool HasMore);

public sealed record MessageDeletedEvent(Guid ChatId, Guid MessageId);
