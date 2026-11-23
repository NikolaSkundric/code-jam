using System.Security.Claims;

namespace CodingJamAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Inbound claim mapping is disabled, so the raw JWT claim names are used.
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue("sub") ?? throw new InvalidOperationException("Missing sub claim."));

    public static Guid GetSessionId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue("jti") ?? throw new InvalidOperationException("Missing jti claim."));
}
