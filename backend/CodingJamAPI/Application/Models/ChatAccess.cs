namespace CodingJamAPI.Application.Models;

/// <summary>
/// What a user may do in a chat. CanWrite is false for a frozen (read-only) chat.
/// CanModerate: may delete other people's messages (room owner / admins).
/// </summary>
public sealed record ChatAccess(bool CanWrite, IReadOnlyList<Guid> ParticipantIds, bool CanModerate = false);
