using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Controllers;

/// <summary>Room invitations received by the current user.</summary>
[ApiController]
[Authorize]
[Route("api/invitations")]
public class InvitationsController(IRoomService rooms) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IncomingInvitationResponse>>> Mine(CancellationToken ct) =>
        Ok(await rooms.GetMyInvitationsAsync(User.GetUserId(), ct));

    [HttpPost("{invitationId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid invitationId, CancellationToken ct)
    {
        await rooms.AcceptInvitationAsync(User.GetUserId(), invitationId, ct);
        return NoContent();
    }

    [HttpPost("{invitationId:guid}/decline")]
    public async Task<IActionResult> Decline(Guid invitationId, CancellationToken ct)
    {
        await rooms.DeclineInvitationAsync(User.GetUserId(), invitationId, ct);
        return NoContent();
    }
}
