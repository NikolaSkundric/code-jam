using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Infrastructure.Persistence;
using CodingJamAPI.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Application.Services;

public sealed class XmppAuthService(AppDbContext db, IPasswordHasher hasher, IOptions<XmppOptions> options) : IXmppAuthService
{
    public async Task<bool> ValidateCredentialsAsync(string user, string server, string password, CancellationToken ct)
    {
        var passwordHash = await FindPasswordHashAsync(user, server, ct);
        // Always verify (against a dummy hash for unknown users) so timing does not reveal which accounts exist.
        var ok = hasher.Verify(password, passwordHash);
        return ok && passwordHash is not null;
    }

    public async Task<bool> UserExistsAsync(string user, string server, CancellationToken ct) =>
        await FindPasswordHashAsync(user, server, ct) is not null;

    private async Task<string?> FindPasswordHashAsync(string user, string server, CancellationToken ct)
    {
        if (!string.Equals(server, options.Value.Domain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // XMPP localparts are case-insensitive and arrive lowercased; web usernames are unique ignoring case.
        var lower = user.ToLowerInvariant();
        return await db.Users.AsNoTracking()
            .Where(u => u.Username.ToLower() == lower)
            .Select(u => u.PasswordHash)
            .FirstOrDefaultAsync(ct);
    }
}
