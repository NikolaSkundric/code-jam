using CodingJamAPI.Application.Models;

namespace CodingJamAPI.Application.Abstractions;

/// <summary>Single place that decides who can read and write in a chat (rooms will plug in here too).</summary>
public interface IChatAccessService
{
    /// <summary>Null when the user cannot read the chat (not a participant / does not exist).</summary>
    Task<ChatAccess?> GetAccessAsync(Guid userId, Guid chatId, CancellationToken ct);
}
