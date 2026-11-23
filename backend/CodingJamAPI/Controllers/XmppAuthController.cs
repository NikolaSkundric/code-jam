using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Controllers;

public sealed record XmppAuthRequest(string User, string Server, string? Password);

/// <summary>Login checks of the Jabber server (ejabberd external auth, xmpp/extauth.py).</summary>
public class XmppAuthController(IXmppAuthService xmpp, IOptions<XmppOptions> options) : XmppInternalControllerBase(options)
{
    [HttpPost("auth")]
    public async Task<IActionResult> Auth(XmppAuthRequest request, CancellationToken ct)
    {
        if (!HasValidSecret()) return NotFound();
        var valid = !string.IsNullOrEmpty(request.Password) &&
                    await xmpp.ValidateCredentialsAsync(request.User, request.Server, request.Password, ct);
        return Ok(new { valid });
    }

    [HttpPost("isuser")]
    public async Task<IActionResult> IsUser(XmppAuthRequest request, CancellationToken ct)
    {
        if (!HasValidSecret()) return NotFound();
        return Ok(new { valid = await xmpp.UserExistsAsync(request.User, request.Server, ct) });
    }
}
