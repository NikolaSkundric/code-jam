namespace CodingJamAPI.Application.Abstractions;

/// <summary>Pushes events to connected users in real time.</summary>
public interface IRealtimeNotifier
{
    /// <summary>Sends an event (with optional payload) to every open tab of the given users.</summary>
    Task NotifyAsync(IEnumerable<Guid> userIds, string eventName, object? payload = null, CancellationToken ct = default);

    /// <summary>Tells the users that their friends list or friend requests changed (clients refetch).</summary>
    Task FriendsChangedAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);
}
