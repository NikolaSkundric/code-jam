using CodingJamAPI.Application.Models;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;

namespace CodingJamAPI.Application.Abstractions;

public interface IMessageService
{
    /// <summary>Newest page when before is null; otherwise the page of messages older than the given message.</summary>
    Task<MessagePageResponse> GetMessagesAsync(Guid userId, Guid chatId, Guid? before, int limit, CancellationToken ct);

    /// <summary>Sends a message with optional attachments. Text may be empty only when files are attached.</summary>
    Task<MessageResponse> SendAsync(
        Guid userId, Guid chatId, SendMessageRequest request, IReadOnlyList<UploadedFile> files, CancellationToken ct);

    /// <summary>A message the author wrote in a Jabber client: stored in the chat and shown live, but not sent back to Jabber.</summary>
    Task<MessageResponse> ReceiveFromJabberAsync(Guid authorId, Guid chatId, string text, CancellationToken ct);

    Task<MessageResponse> EditAsync(Guid userId, Guid chatId, Guid messageId, EditMessageRequest request, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid chatId, Guid messageId, CancellationToken ct);

    /// <summary>Marks everything in the chat as read for the user (clears the unread indicator).</summary>
    Task MarkReadAsync(Guid userId, Guid chatId, CancellationToken ct);
}
