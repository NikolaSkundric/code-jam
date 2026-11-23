using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/jabber")]
public class JabberController(IOptions<XmppOptions> options) : ControllerBase
{
    /// <summary>The Jabber domain and client port, so the Settings page can tell users how to connect.</summary>
    [HttpGet("info")]
    public ActionResult<JabberInfoResponse> Info() => Ok(new JabberInfoResponse(options.Value.Domain, options.Value.ClientPort));
}
