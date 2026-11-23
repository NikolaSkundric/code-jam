using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/bans")]
public class BansController(IBanService bans) : ControllerBase
{
    /// <summary>Users the current user has banned.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BannedUserResponse>>> List(CancellationToken ct) =>
        Ok(await bans.GetBannedAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<IActionResult> Ban(BanUserRequest request, CancellationToken ct)
    {
        await bans.BanAsync(User.GetUserId(), request, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Unban(Guid userId, CancellationToken ct)
    {
        await bans.UnbanAsync(User.GetUserId(), userId, ct);
        return NoContent();
    }
}
