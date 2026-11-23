using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Domain.Enums;
using CodingJamAPI.Options;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Infrastructure.Realtime;

public sealed class PresenceTracker(IOptions<PresenceOptions> options, TimeProvider clock) : IPresenceTracker
{
    private sealed class Tab(Guid userId, DateTimeOffset lastActivity)
    {
        public Guid UserId { get; } = userId;
        public DateTimeOffset LastActivity { get; set; } = lastActivity;
    }

    private sealed class UserState(string username)
    {
        public string Username { get; } = username;
        public PresenceStatus Published { get; set; } = PresenceStatus.Offline;
        public HashSet<string> Connections { get; } = new();
    }

    private readonly TimeSpan _afkAfter = TimeSpan.FromSeconds(options.Value.AfkAfterSeconds);
    private readonly object _lock = new();
    private readonly Dictionary<string, Tab> _tabs = new();
    private readonly Dictionary<Guid, UserState> _users = new();

    public PresenceChange? Connect(Guid userId, string username, string connectionId)
    {
        lock (_lock)
        {
            // Opening a tab counts as activity.
            _tabs[connectionId] = new Tab(userId, clock.GetUtcNow());
            if (!_users.TryGetValue(userId, out var user))
            {
                user = new UserState(username);
                _users[userId] = user;
            }
            user.Connections.Add(connectionId);
            return Recompute(userId, user);
        }
    }

    public PresenceChange? Activity(string connectionId)
    {
        lock (_lock)
        {
            if (!_tabs.TryGetValue(connectionId, out var tab))
            {
                return null;
            }
            tab.LastActivity = clock.GetUtcNow();
            return _users.TryGetValue(tab.UserId, out var user) ? Recompute(tab.UserId, user) : null;
        }
    }

    public PresenceChange? Disconnect(string connectionId)
    {
        lock (_lock)
        {
            if (!_tabs.Remove(connectionId, out var tab) || !_users.TryGetValue(tab.UserId, out var user))
            {
                return null;
            }

            user.Connections.Remove(connectionId);
            var change = Recompute(tab.UserId, user);
            if (user.Connections.Count == 0)
            {
                _users.Remove(tab.UserId);
            }
            return change;
        }
    }

    public IReadOnlyList<PresenceChange> Sweep()
    {
        lock (_lock)
        {
            var changes = new List<PresenceChange>();
            foreach (var (userId, user) in _users)
            {
                var change = Recompute(userId, user);
                if (change is not null)
                {
                    changes.Add(change);
                }
            }
            return changes;
        }
    }

    public IReadOnlyList<PresenceChange> GetSnapshot()
    {
        lock (_lock)
        {
            return _users
                .Select(kv => new PresenceChange(kv.Key, kv.Value.Username, Compute(kv.Value)))
                .Where(p => p.Status != PresenceStatus.Offline)
                .OrderBy(p => p.Username, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    // Caller holds the lock.
    private PresenceStatus Compute(UserState user)
    {
        if (user.Connections.Count == 0)
        {
            return PresenceStatus.Offline;
        }

        var now = clock.GetUtcNow();
        var anyActive = user.Connections.Any(id => _tabs.TryGetValue(id, out var tab) && now - tab.LastActivity <= _afkAfter);
        return anyActive ? PresenceStatus.Online : PresenceStatus.Afk;
    }

    // Caller holds the lock.
    private PresenceChange? Recompute(Guid userId, UserState user)
    {
        var status = Compute(user);
        if (status == user.Published)
        {
            return null;
        }

        user.Published = status;
        return new PresenceChange(userId, user.Username, status);
    }
}
