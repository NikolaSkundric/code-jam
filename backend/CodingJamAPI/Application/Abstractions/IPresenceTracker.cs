using CodingJamAPI.Application.Models;

namespace CodingJamAPI.Application.Abstractions;

/// <summary>
/// Tracks which browser tabs (SignalR connections) each user has open and when each was last active.
/// User status: Online if any tab was active within the AFK window, Afk if tabs are open but all
/// idle, Offline when no tab is open. Each method returns the status changes it caused.
/// </summary>
public interface IPresenceTracker
{
    PresenceChange? Connect(Guid userId, string username, string connectionId);

    /// <summary>The tab saw user interaction.</summary>
    PresenceChange? Activity(string connectionId);

    PresenceChange? Disconnect(string connectionId);

    /// <summary>Detects tabs that became idle; call periodically.</summary>
    IReadOnlyList<PresenceChange> Sweep();

    /// <summary>All users that are currently Online or Afk.</summary>
    IReadOnlyList<PresenceChange> GetSnapshot();
}
