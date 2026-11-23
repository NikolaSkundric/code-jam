using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Extensions;
using CodingJamAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CodingJamAPI.Hubs;

/// <summary>
/// One connection per browser tab. Tabs call Activity() when the user interacts; the server decides
/// Online / AFK / Offline across all tabs of the user (see IPresenceTracker). A status change is sent
/// only to the user themselves and to the people allowed to see it (friends and room mates), and
/// GetSnapshot returns only those people.
/// The same connection also carries the per-user push events (friends, messages, rooms).
/// </summary>
[Authorize]
public sealed class PresenceHub(IPresenceTracker tracker, AppDbContext db, IPresenceAudienceService audience) : Hub
{
    public const string PresenceChangedEvent = "PresenceChanged";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User!.GetUserId();
        var username = await db.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstAsync();

        var change = tracker.Connect(userId, username, Context.ConnectionId);
        await Publish(change);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Publish(tracker.Disconnect(Context.ConnectionId));
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>The user interacted with this tab.</summary>
    public Task Activity() => Publish(tracker.Activity(Context.ConnectionId));

    /// <summary>Contacts and room mates that are currently Online or AFK, plus the caller.</summary>
    public async Task<IReadOnlyList<PresenceChange>> GetSnapshot()
    {
        var userId = Context.User!.GetUserId();
        var visible = await audience.GetVisibleUsersAsync(userId, Context.ConnectionAborted);
        visible.Add(userId);
        return tracker.GetSnapshot().Where(p => visible.Contains(p.UserId)).ToList();
    }

    private async Task Publish(PresenceChange? change)
    {
        if (change is null)
        {
            return;
        }

        var recipients = await audience.GetVisibleUsersAsync(change.UserId, Context.ConnectionAborted);
        recipients.Add(change.UserId);
        await Clients.Users(recipients.Select(id => id.ToString()).ToList()).SendAsync(PresenceChangedEvent, change);
    }
}
