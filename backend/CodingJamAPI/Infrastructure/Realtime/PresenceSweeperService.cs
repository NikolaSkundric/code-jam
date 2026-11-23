using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Options;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Infrastructure.Realtime;

/// <summary>Periodically detects users whose tabs all became idle and tells the people who may see their status.</summary>
public sealed class PresenceSweeperService(
    IPresenceTracker tracker,
    IPresencePublisher publisher,
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
                    await publisher.PublishAsync(change, stoppingToken);
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
