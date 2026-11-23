namespace CodingJamAPI.Contracts.Responses;

public sealed record BannedUserResponse(Guid UserId, string Username, DateTime BannedAt);
