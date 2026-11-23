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
/// Online / AFK / Offline across all tabs of the user (see IPresenceTracker).
/// NOTE: changes are currently broadcast to every connected user; restrict them to the user's
/// contacts and shared rooms once those exist.
/// </summary>
[Authorize]
public sealed class PresenceHub(IPresenceTracker tracker, AppDbContext db) : Hub
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

    /// <summary>Everyone currently Online or AFK.</summary>
    public IReadOnlyList<PresenceChange> GetSnapshot() => tracker.GetSnapshot();

    private Task Publish(PresenceChange? change) =>
        change is null ? Task.CompletedTask : Clients.All.SendAsync(PresenceChangedEvent, change);
}
