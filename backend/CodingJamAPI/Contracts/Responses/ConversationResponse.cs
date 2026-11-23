namespace CodingJamAPI.Contracts.Responses;

/// <summary>A personal dialog as shown in the conversation list.</summary>
public sealed record ConversationResponse(
    Guid ChatId,
    Guid UserId,
    string Username,
    string? LastMessageText,
    DateTime? LastMessageAt,
    int UnreadCount,
    /// <summary>False when the chat is frozen (not friends any more, or banned): history only.</summary>
    bool CanWrite);
