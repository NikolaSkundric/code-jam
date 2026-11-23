using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CodingJamAPI.Infrastructure.Realtime;

/// <summary>Event names understood by the frontend (features/presence PresenceProvider).</summary>
public static class RealtimeEvents
{
    public const string FriendsChanged = "FriendsChanged";
    public const string MessageReceived = "MessageReceived";
    public const string MessageEdited = "MessageEdited";
    public const string MessageDeleted = "MessageDeleted";
    public const string ConversationsChanged = "ConversationsChanged";

    /// <summary>Membership, roles, settings, bans or invitations of a room changed (payload: room id).</summary>
    public const string RoomsChanged = "RoomsChanged";
}

public sealed class SignalRNotifier(IHubContext<PresenceHub> hub) : IRealtimeNotifier
{
    public Task NotifyAsync(IEnumerable<Guid> userIds, string eventName, object? payload = null, CancellationToken ct = default) =>
        hub.Clients.Users(userIds.Distinct().Select(id => id.ToString()).ToList())
            .SendAsync(eventName, payload, ct);

    public Task FriendsChangedAsync(IEnumerable<Guid> userIds, CancellationToken ct = default) =>
        NotifyAsync(userIds, RealtimeEvents.FriendsChanged, null, ct);
}
