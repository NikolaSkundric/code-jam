using CodingJamAPI.Contracts.Responses;

namespace CodingJamAPI.Application.Abstractions;

public interface IPersonalChatService
{
    /// <summary>All personal dialogs of the user, including frozen ones, newest activity first.</summary>
    Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(Guid userId, CancellationToken ct);

    /// <summary>Opens (creating it if needed) the dialog with a friend.</summary>
    Task<ConversationResponse> OpenAsync(Guid userId, Guid friendId, CancellationToken ct);
}
