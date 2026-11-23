using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CodingJamAPI.Infrastructure.Persistence;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CodingJamAPI.Extensions;

public static class AuthenticationExtensions
{
    private static readonly TimeSpan LastSeenInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// JWT Bearer authentication where the token is read from an HttpOnly cookie, and every request
    /// is checked against the server-side session (jti) so logout / revoke take effect immediately.
    /// </summary>
    public static IServiceCollection AddJwtCookieAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        // Without a configured key (JWT_SIGNING_KEY in .env) a random one is used for this run:
        // everything works, but sessions end whenever the API restarts. No key is stored in the repository.
        var signingKey = jwt.SigningKey.Length >= 32
            ? jwt.SigningKey
            : Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.PostConfigure<JwtOptions>(o =>
        {
            if (o.SigningKey.Length < 32)
            {
                o.SigningKey = signingKey;
            }
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep raw "sub" / "jti" claim names
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub"
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Token lives in the HttpOnly cookie (also covers SignalR connections).
                        if (context.Request.Cookies.TryGetValue(jwt.CookieName, out var token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal!;
                        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId) ||
                            !Guid.TryParse(principal.FindFirstValue("jti"), out var sessionId))
                        {
                            context.Fail("Malformed token.");
                            return;
                        }

                        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var now = DateTime.UtcNow;

                        // Session must exist, be active, and belong to a user that is not deleted.
                        var lastSeen = await db.UserSessions
                            .Where(s => s.Id == sessionId && s.UserId == userId
                                        && s.RevokedAt == null && s.ExpiresAt > now
                                        && db.Users.Any(u => u.Id == s.UserId))
                            .Select(s => (DateTime?)s.LastSeenAt)
                            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                        if (lastSeen is null)
                        {
                            context.Fail("Session is no longer valid.");
                            return;
                        }

                        if (now - lastSeen.Value > LastSeenInterval)
                        {
                            await db.UserSessions
                                .Where(s => s.Id == sessionId)
                                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSeenAt, now),
                                    context.HttpContext.RequestAborted);
                        }
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }
}
