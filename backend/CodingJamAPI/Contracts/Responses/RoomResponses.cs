using CodingJamAPI.Domain.Enums;

namespace CodingJamAPI.Contracts.Responses;

/// <summary>A room the user is a member of (sidebar). Id is also the chat id for the message endpoints.</summary>
public sealed record RoomSummaryResponse(
    Guid Id,
    string Name,
    string Description,
    RoomVisibility Visibility,
    int MemberCount,
    RoomRole MyRole,
    int UnreadCount);

/// <summary>An entry of the public catalog.</summary>
public sealed record RoomCatalogItemResponse(Guid Id, string Name, string Description, int MemberCount, bool IsMember);

public sealed record RoomMemberResponse(Guid UserId, string Username, RoomRole Role, DateTime JoinedAt);

public sealed record RoomDetailsResponse(
    Guid Id,
    string Name,
    string Description,
    RoomVisibility Visibility,
    Guid OwnerId,
    string OwnerUsername,
    RoomRole MyRole,
    IReadOnlyList<RoomMemberResponse> Members);

public sealed record RoomBanResponse(Guid UserId, string Username, Guid BannedById, string BannedByUsername, DateTime BannedAt);

/// <summary>A pending invitation as seen by the room admins.</summary>
public sealed record RoomInvitationResponse(Guid Id, Guid UserId, string Username, string InvitedByUsername, DateTime CreatedAt);

/// <summary>An invitation received by the current user.</summary>
public sealed record IncomingInvitationResponse(Guid Id, Guid RoomId, string RoomName, string InvitedByUsername, DateTime CreatedAt);
