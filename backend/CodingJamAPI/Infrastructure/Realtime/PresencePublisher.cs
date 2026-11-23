using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CodingJamAPI.Infrastructure.Realtime;

public sealed class PresencePublisher(IHubContext<PresenceHub> hub, IServiceScopeFactory scopes) : IPresencePublisher
{
    public async Task PublishAsync(PresenceChange change, CancellationToken ct = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var audience = scope.ServiceProvider.GetRequiredService<IPresenceAudienceService>();

        var recipients = await audience.GetVisibleUsersAsync(change.UserId, ct);
        recipients.Add(change.UserId);
        await hub.Clients.Users(recipients.Select(id => id.ToString()).ToList())
            .SendAsync(PresenceHub.PresenceChangedEvent, change, ct);
    }
}
