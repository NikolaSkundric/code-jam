using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/friends")]
public class FriendsController(IFriendService friends) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FriendResponse>>> List(CancellationToken ct) =>
        Ok(await friends.GetFriendsAsync(User.GetUserId(), ct));

    [HttpDelete("{friendId:guid}")]
    public async Task<IActionResult> Remove(Guid friendId, CancellationToken ct)
    {
        await friends.RemoveFriendAsync(User.GetUserId(), friendId, ct);
        return NoContent();
    }

    [HttpGet("requests")]
    public async Task<ActionResult<FriendRequestsResponse>> Requests(CancellationToken ct) =>
        Ok(await friends.GetRequestsAsync(User.GetUserId(), ct));

    [HttpPost("requests")]
    public async Task<IActionResult> Send(SendFriendRequestRequest request, CancellationToken ct)
    {
        await friends.SendRequestAsync(User.GetUserId(), request, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("requests/{requestId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid requestId, CancellationToken ct)
    {
        await friends.AcceptRequestAsync(User.GetUserId(), requestId, ct);
        return NoContent();
    }

    [HttpPost("requests/{requestId:guid}/decline")]
    public async Task<IActionResult> Decline(Guid requestId, CancellationToken ct)
    {
        await friends.DeclineRequestAsync(User.GetUserId(), requestId, ct);
        return NoContent();
    }

    /// <summary>The sender withdraws a pending request.</summary>
    [HttpDelete("requests/{requestId:guid}")]
    public async Task<IActionResult> Cancel(Guid requestId, CancellationToken ct)
    {
        await friends.CancelRequestAsync(User.GetUserId(), requestId, ct);
        return NoContent();
    }
}
