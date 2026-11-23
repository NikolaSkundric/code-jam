using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Hubs;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Infrastructure.Realtime;

/// <summary>Periodically detects users whose tabs all became idle and tells the people who may see their status.</summary>
public sealed class PresenceSweeperService(
    IPresenceTracker tracker,
    IHubContext<PresenceHub> hub,
    IServiceScopeFactory scopeFactory,
    IOptions<PresenceOptions> options,
    ILogger<PresenceSweeperService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.SweepIntervalMilliseconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var changes = tracker.Sweep();
                if (changes.Count == 0)
                {
                    continue;
                }

                await using var scope = scopeFactory.CreateAsyncScope();
                var audience = scope.ServiceProvider.GetRequiredService<IPresenceAudienceService>();
                foreach (var change in changes)
                {
                    var recipients = await audience.GetVisibleUsersAsync(change.UserId, stoppingToken);
                    recipients.Add(change.UserId);
                    await hub.Clients.Users(recipients.Select(id => id.ToString()).ToList())
                        .SendAsync(PresenceHub.PresenceChangedEvent, change, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Presence sweep failed");
            }
        }
    }
}
