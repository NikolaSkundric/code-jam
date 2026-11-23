namespace CodingJamAPI.Contracts.Responses;

public sealed record FriendResponse(Guid UserId, string Username, DateTime FriendsSince);
