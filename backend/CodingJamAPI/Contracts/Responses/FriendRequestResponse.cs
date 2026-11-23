namespace CodingJamAPI.Contracts.Responses;

/// <summary>A pending friend request; UserId/Username describe the other person.</summary>
public sealed record FriendRequestResponse(Guid Id, Guid UserId, string Username, string? Message, DateTime CreatedAt);

public sealed record FriendRequestsResponse(
    IReadOnlyList<FriendRequestResponse> Incoming,
    IReadOnlyList<FriendRequestResponse> Outgoing);
