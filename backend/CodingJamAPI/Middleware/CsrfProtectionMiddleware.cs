using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Middleware;

/// <summary>
/// CSRF defence for cookie-based auth: state-changing API requests must carry a custom header.
/// Browsers cannot send it cross-site without a CORS preflight, which this API never allows.
/// Combined with SameSite=Lax cookies.
/// </summary>
public sealed class CsrfProtectionMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Requested-With";

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var isSafe = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) ||
                     HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);

        if (!isSafe && context.Request.Path.StartsWithSegments("/api") &&
            !context.Request.Headers.ContainsKey(HeaderName))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = 403, Title = "Forbidden", Detail = $"Missing {HeaderName} header." },
                options: null, contentType: "application/problem+json");
            return;
        }

        await next(context);
    }
}
