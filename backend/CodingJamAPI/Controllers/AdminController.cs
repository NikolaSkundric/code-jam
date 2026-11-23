using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Controllers;

/// <summary>Administration pages. Only users listed in the Admin:Usernames setting may call these.</summary>
[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController(IAuthService auth, IXmppAdminService xmpp) : ControllerBase
{
    /// <summary>Jabber connection dashboard and federation traffic (TASK.md section 6).</summary>
    [HttpGet("jabber")]
    public async Task<ActionResult<XmppDashboardResponse>> Jabber(CancellationToken ct)
    {
        var user = await auth.GetCurrentUserAsync(User.GetUserId(), ct);
        if (!user.IsAdmin)
        {
            return Forbid();
        }
        return Ok(await xmpp.GetDashboardAsync(ct));
    }
}
