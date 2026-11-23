using System.Security.Cryptography;
using System.Text;
using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Controllers;

public sealed record XmppAuthRequest(string User, string Server, string? Password);

/// <summary>
/// Internal endpoints called only by the ejabberd container (xmpp/extauth.py), protected by a shared
/// secret header. Not part of the public /api surface.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("internal/xmpp")]
public class XmppAuthController(IXmppAuthService xmpp, IOptions<XmppOptions> options) : ControllerBase
{
    private const string SecretHeader = "X-Xmpp-Auth-Secret";

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

    // Constant-time comparison; with no secret configured the endpoints do not exist.
    private bool HasValidSecret()
    {
        var expected = options.Value.AuthSecret;
        if (string.IsNullOrEmpty(expected) || !Request.Headers.TryGetValue(SecretHeader, out var given))
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(given.ToString())));
    }
}
