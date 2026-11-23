using System.Security.Cryptography;
using System.Text;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Controllers;

/// <summary>
/// Base of the internal endpoints called only by the Jabber server container, protected by a shared secret
/// header. They are not part of the public /api surface.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("internal/xmpp")]
public abstract class XmppInternalControllerBase(IOptions<XmppOptions> options) : ControllerBase
{
    private const string SecretHeader = "X-Xmpp-Auth-Secret";

    // Constant-time comparison; with no secret configured the endpoints do not exist.
    protected bool HasValidSecret()
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
