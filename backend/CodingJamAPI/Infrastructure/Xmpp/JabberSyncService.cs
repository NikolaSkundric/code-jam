using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodingJamAPI.Infrastructure.Xmpp;

/// <summary>
/// After startup (the Jabber server may still be booting): puts every friendship into the Jabber rosters and
/// registers the Jabber sessions that already exist. Retries until the Jabber server answers.
/// </summary>
public sealed class JabberSyncService(IServiceScopeFactory scopes, ILogger<JabberSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            for (var attempt = 1; attempt <= 18 && !stoppingToken.IsCancellationRequested; attempt++)
            {
                await using var scope = scopes.CreateAsyncScope();
                var gateway = scope.ServiceProvider.GetRequiredService<IJabberGateway>();
                if (await gateway.IsAvailableAsync(stoppingToken))
                {
                    await SyncAsync(scope.ServiceProvider, gateway, stoppingToken);
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            logger.LogWarning("The Jabber server did not become available; rosters were not synchronised");
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Jabber synchronisation failed");
        }
    }

    private async Task SyncAsync(IServiceProvider services, IJabberGateway gateway, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var pairs = await (
            from f in db.Friendships.AsNoTracking()
            join a in db.Users.AsNoTracking() on f.UserAId equals a.Id
            join b in db.Users.AsNoTracking() on f.UserBId equals b.Id
            select new { A = a.Username, B = b.Username }).ToListAsync(ct);

        foreach (var pair in pairs)
        {
            await gateway.AddFriendshipAsync(pair.A, pair.B);
        }

        await services.GetRequiredService<IXmppBridgeService>().SyncSessionsAsync(ct);
        logger.LogInformation("Jabber rosters synchronised for {Count} friendships", pairs.Count);
    }
}
