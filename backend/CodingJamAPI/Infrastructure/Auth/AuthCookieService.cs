using CodingJamAPI.Options;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Infrastructure.Auth;

public interface IAuthCookieService
{
    void Append(HttpResponse response, string token, DateTime expiresAtUtc, bool persistent);
    void Delete(HttpResponse response);
}

/// <summary>Writes the JWT to an HttpOnly cookie (never readable from JavaScript).</summary>
public sealed class AuthCookieService(IOptions<JwtOptions> options) : IAuthCookieService
{
    private readonly JwtOptions _options = options.Value;

    public void Append(HttpResponse response, string token, DateTime expiresAtUtc, bool persistent)
    {
        var cookie = BuildOptions(response.HttpContext);
        if (persistent)
        {
            // Persistent cookie survives browser restarts; otherwise it is a session cookie.
            cookie.Expires = new DateTimeOffset(expiresAtUtc, TimeSpan.Zero);
        }
        response.Cookies.Append(_options.CookieName, token, cookie);
    }

    public void Delete(HttpResponse response) =>
        response.Cookies.Delete(_options.CookieName, BuildOptions(response.HttpContext));

    private static CookieOptions BuildOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true
    };
}
