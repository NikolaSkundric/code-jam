using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;

namespace CodingJamAPI.Application.Abstractions;

public interface IRoomService
{
    Task<RoomDetailsResponse> CreateAsync(Guid userId, CreateRoomRequest request, CancellationToken ct);
    Task<IReadOnlyList<RoomSummaryResponse>> GetMyRoomsAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<RoomCatalogItemResponse>> GetCatalogAsync(Guid userId, string? search, CancellationToken ct);
    Task<RoomDetailsResponse> GetAsync(Guid userId, Guid roomId, CancellationToken ct);

    Task<RoomDetailsResponse> UpdateAsync(Guid userId, Guid roomId, UpdateRoomRequest request, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid roomId, CancellationToken ct);

    Task JoinAsync(Guid userId, Guid roomId, CancellationToken ct);
    Task LeaveAsync(Guid userId, Guid roomId, CancellationToken ct);

    Task SetAdminAsync(Guid userId, Guid roomId, Guid targetUserId, bool isAdmin, CancellationToken ct);
    Task BanMemberAsync(Guid userId, Guid roomId, Guid targetUserId, CancellationToken ct);
    Task<IReadOnlyList<RoomBanResponse>> GetBansAsync(Guid userId, Guid roomId, CancellationToken ct);
    Task UnbanAsync(Guid userId, Guid roomId, Guid targetUserId, CancellationToken ct);

    Task InviteAsync(Guid userId, Guid roomId, InviteToRoomRequest request, CancellationToken ct);
    Task<IReadOnlyList<RoomInvitationResponse>> GetRoomInvitationsAsync(Guid userId, Guid roomId, CancellationToken ct);
    Task CancelInvitationAsync(Guid userId, Guid roomId, Guid invitationId, CancellationToken ct);

    Task<IReadOnlyList<IncomingInvitationResponse>> GetMyInvitationsAsync(Guid userId, CancellationToken ct);
    Task AcceptInvitationAsync(Guid userId, Guid invitationId, CancellationToken ct);
    Task DeclineInvitationAsync(Guid userId, Guid invitationId, CancellationToken ct);
}
