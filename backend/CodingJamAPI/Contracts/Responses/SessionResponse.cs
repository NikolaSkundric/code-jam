namespace CodingJamAPI.Contracts.Responses;

public sealed record SessionResponse(
    Guid Id,
    string UserAgent,
    string IpAddress,
    DateTime CreatedAt,
    DateTime LastSeenAt,
    bool IsCurrent);
