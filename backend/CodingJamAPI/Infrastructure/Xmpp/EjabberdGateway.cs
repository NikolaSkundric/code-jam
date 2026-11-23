using System.Text.Json;
using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Options;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Infrastructure.Xmpp;

public sealed class EjabberdGateway(
    EjabberdApiClient api,
    IOptions<XmppOptions> options,
    ILogger<EjabberdGateway> logger) : IJabberGateway
{
    private readonly XmppOptions _options = options.Value;

    private bool Enabled => !string.IsNullOrWhiteSpace(_options.AdminApiUrl);
    private string Jid(string username) => $"{username.ToLowerInvariant()}@{_options.Domain}";

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!Enabled) return false;
        try
        {
            await api.CommandAsync(_options.AdminApiUrl, "status", new { }, ct);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    public Task SendChatMessageAsync(string fromUsername, string toUsername, string body) =>
        TryAsync("send_message", new { type = "chat", from = Jid(fromUsername), to = Jid(toUsername), subject = "", body });

    public async Task AddFriendshipAsync(string usernameA, string usernameB)
    {
        await AddRosterItemAsync(usernameA, usernameB);
        await AddRosterItemAsync(usernameB, usernameA);
    }

    public async Task RemoveFriendshipAsync(string usernameA, string usernameB)
    {
        await TryAsync("delete_rosteritem", RosterKey(usernameA, usernameB));
        await TryAsync("delete_rosteritem", RosterKey(usernameB, usernameA));
    }

    public Task KickUserAsync(string username) =>
        TryAsync("kick_user", new { user = username.ToLowerInvariant(), host = _options.Domain });

    public async Task<IReadOnlyList<(string Username, string Resource)>> GetSessionsAsync(CancellationToken ct = default)
    {
        var result = new List<(string, string)>();
        if (!Enabled) return result;
        try
        {
            var list = await api.CommandAsync(_options.AdminApiUrl, "connected_users_info", new { }, ct);
            if (list.ValueKind != JsonValueKind.Array) return result;
            foreach (var item in list.EnumerateArray())
            {
                var session = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("session", out var inner) ? inner : item;
                if (session.ValueKind != JsonValueKind.Object || !session.TryGetProperty("jid", out var jid)) continue;

                // jid looks like user@domain/resource
                var text = jid.GetString() ?? "";
                var at = text.IndexOf('@');
                var slash = text.IndexOf('/');
                if (at > 0 && slash > at) result.Add((text[..at], text[(slash + 1)..]));
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Could not read the Jabber sessions");
        }
        return result;
    }

    private object RosterKey(string owner, string contact) =>
        new { localuser = owner.ToLowerInvariant(), localhost = _options.Domain, user = contact.ToLowerInvariant(), host = _options.Domain };

    private Task AddRosterItemAsync(string owner, string contact) =>
        TryAsync("add_rosteritem", new
        {
            localuser = owner.ToLowerInvariant(),
            localhost = _options.Domain,
            user = contact.ToLowerInvariant(),
            host = _options.Domain,
            nick = contact,
            groups = new[] { "Friends" },
            subs = "both"
        });

    private async Task TryAsync(string command, object body)
    {
        if (!Enabled) return;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await api.CommandAsync(_options.AdminApiUrl, command, body, timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Jabber command {Command} failed", command);
        }
    }
}
