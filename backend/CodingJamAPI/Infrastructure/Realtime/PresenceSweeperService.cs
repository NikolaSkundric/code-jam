using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Hubs;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Infrastructure.Realtime;

/// <summary>Periodically detects users whose tabs all became idle and broadcasts the change.</summary>
public sealed class PresenceSweeperService(
    IPresenceTracker tracker,
    IHubContext<PresenceHub> hub,
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
                foreach (var change in tracker.Sweep())
                {
                    await hub.Clients.All.SendAsync(PresenceHub.PresenceChangedEvent, change, stoppingToken);
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
