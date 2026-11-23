using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Controllers;

/// <summary>Rooms. A room id is also its chat id: messages use /api/chats/{roomId}/messages.</summary>
[ApiController]
[Authorize]
[Route("api/rooms")]
public class RoomsController(IRoomService rooms) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomSummaryResponse>>> Mine(CancellationToken ct) =>
        Ok(await rooms.GetMyRoomsAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<RoomDetailsResponse>> Create(CreateRoomRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await rooms.CreateAsync(User.GetUserId(), request, ct));

    /// <summary>Public rooms with simple search by name or description.</summary>
    [HttpGet("catalog")]
    public async Task<ActionResult<IReadOnlyList<RoomCatalogItemResponse>>> Catalog([FromQuery] string? search, CancellationToken ct) =>
        Ok(await rooms.GetCatalogAsync(User.GetUserId(), search, ct));

    [HttpGet("{roomId:guid}")]
    public async Task<ActionResult<RoomDetailsResponse>> Get(Guid roomId, CancellationToken ct) =>
        Ok(await rooms.GetAsync(User.GetUserId(), roomId, ct));

    [HttpPut("{roomId:guid}")]
    public async Task<ActionResult<RoomDetailsResponse>> Update(Guid roomId, UpdateRoomRequest request, CancellationToken ct) =>
        Ok(await rooms.UpdateAsync(User.GetUserId(), roomId, request, ct));

    [HttpDelete("{roomId:guid}")]
    public async Task<IActionResult> Delete(Guid roomId, CancellationToken ct)
    {
        await rooms.DeleteAsync(User.GetUserId(), roomId, ct);
        return NoContent();
    }

    [HttpPost("{roomId:guid}/join")]
    public async Task<IActionResult> Join(Guid roomId, CancellationToken ct)
    {
        await rooms.JoinAsync(User.GetUserId(), roomId, ct);
        return NoContent();
    }

    [HttpPost("{roomId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid roomId, CancellationToken ct)
    {
        await rooms.LeaveAsync(User.GetUserId(), roomId, ct);
        return NoContent();
    }

    [HttpPut("{roomId:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> SetRole(Guid roomId, Guid userId, SetRoomRoleRequest request, CancellationToken ct)
    {
        await rooms.SetAdminAsync(User.GetUserId(), roomId, userId, request.IsAdmin, ct);
        return NoContent();
    }

    [HttpGet("{roomId:guid}/bans")]
    public async Task<ActionResult<IReadOnlyList<RoomBanResponse>>> Bans(Guid roomId, CancellationToken ct) =>
        Ok(await rooms.GetBansAsync(User.GetUserId(), roomId, ct));

    /// <summary>Removes a member and puts them on the ban list.</summary>
    [HttpPost("{roomId:guid}/bans")]
    public async Task<IActionResult> Ban(Guid roomId, BanRoomMemberRequest request, CancellationToken ct)
    {
        await rooms.BanMemberAsync(User.GetUserId(), roomId, request.UserId, ct);
        return NoContent();
    }

    [HttpDelete("{roomId:guid}/bans/{userId:guid}")]
    public async Task<IActionResult> Unban(Guid roomId, Guid userId, CancellationToken ct)
    {
        await rooms.UnbanAsync(User.GetUserId(), roomId, userId, ct);
        return NoContent();
    }

    [HttpGet("{roomId:guid}/invitations")]
    public async Task<ActionResult<IReadOnlyList<RoomInvitationResponse>>> Invitations(Guid roomId, CancellationToken ct) =>
        Ok(await rooms.GetRoomInvitationsAsync(User.GetUserId(), roomId, ct));

    [HttpPost("{roomId:guid}/invitations")]
    public async Task<IActionResult> Invite(Guid roomId, InviteToRoomRequest request, CancellationToken ct)
    {
        await rooms.InviteAsync(User.GetUserId(), roomId, request, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpDelete("{roomId:guid}/invitations/{invitationId:guid}")]
    public async Task<IActionResult> CancelInvitation(Guid roomId, Guid invitationId, CancellationToken ct)
    {
        await rooms.CancelInvitationAsync(User.GetUserId(), roomId, invitationId, ct);
        return NoContent();
    }
}
