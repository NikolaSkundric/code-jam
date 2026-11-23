using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Options;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Infrastructure.Xmpp;

/// <summary>
/// Talks to the internal management API of ejabberd (POST /api/command with a JSON body) and to the
/// federation traffic counters of mod_chat_stats (GET /stats). Never reachable from outside the compose network.
/// </summary>
public sealed partial class EjabberdAdminService(
    HttpClient http,
    IOptions<XmppOptions> options,
    ILogger<EjabberdAdminService> logger) : IXmppAdminService
{
    private readonly XmppOptions _options = options.Value;

    public async Task<XmppDashboardResponse> GetDashboardAsync(CancellationToken ct)
    {
        var local = ReadServerAsync("A", _options.Domain, _options.AdminApiUrl, ct);
        var peer = string.IsNullOrWhiteSpace(_options.PeerAdminApiUrl)
            ? Task.FromResult<XmppServerResponse?>(null)
            : ReadPeerAsync(ct);

        return new XmppDashboardResponse(await local, await peer, DateTime.UtcNow);
    }

    private async Task<XmppServerResponse?> ReadPeerAsync(CancellationToken ct) =>
        await ReadServerAsync("B", _options.PeerDomain, _options.PeerAdminApiUrl, ct);

    private async Task<XmppServerResponse> ReadServerAsync(string name, string domain, string baseUrl, CancellationToken ct)
    {
        return await ReadServerCoreAsync(name, domain, baseUrl, ct);
    }

    private async Task<XmppServerResponse> ReadServerCoreAsync(string name, string domain, string baseUrl, CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(4));
            var token = timeout.Token;
            var root = baseUrl.TrimEnd('/');

            var status = await CommandAsync(root, "status", new { }, token);
            var uptime = await CommandAsync(root, "stats", new { name = "uptimeseconds" }, token);
            var connected = await CommandAsync(root, "connected_users_number", new { }, token);
            var incoming = await CommandAsync(root, "incoming_s2s_number", new { }, token);
            var outgoing = await CommandAsync(root, "outgoing_s2s_number", new { }, token);
            var sessions = await CommandAsync(root, "connected_users_info", new { }, token);
            var traffic = await http.GetStringAsync($"{root}/stats", token);

            return new XmppServerResponse(
                name, domain, true, null,
                ParseVersion(status.ValueKind == JsonValueKind.String ? status.GetString() : null),
                AsLong(uptime), (int)AsLong(connected), (int)AsLong(incoming), (int)AsLong(outgoing),
                ParseSessions(sessions), ParseTraffic(traffic));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Jabber server {Domain} is not reachable", domain);
            return new XmppServerResponse(name, domain, false, "The server is not reachable.", null, 0, 0, 0, 0, [], []);
        }
    }

    private async Task<JsonElement> CommandAsync(string root, string command, object body, CancellationToken ct)
    {
        // StringContent has a Content-Length; ejabberd answers 400 to the chunked body PostAsJsonAsync would send.
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync($"{root}/api/{command}", content, ct);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return document.RootElement.Clone();
    }

    private static long AsLong(JsonElement element) =>
        element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var value) ? value : 0;

    private static string? ParseVersion(string? status)
    {
        var match = status is null ? null : VersionPattern().Match(status);
        return match is { Success: true } ? match.Groups[1].Value : null;
    }

    /// <summary>connected_users_info returns a list of sessions (each possibly wrapped in a "session" object).</summary>
    private static List<XmppSessionResponse> ParseSessions(JsonElement list)
    {
        var result = new List<XmppSessionResponse>();
        if (list.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in list.EnumerateArray())
        {
            var session = item.ValueKind == JsonValueKind.Object && item.TryGetProperty("session", out var inner) ? inner : item;
            if (session.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            result.Add(new XmppSessionResponse(
                Text(session, "jid"), Text(session, "connection"), Text(session, "ip"),
                (int)Number(session, "port"), (int)Number(session, "priority"),
                Number(session, "uptime"), Text(session, "status")));
        }
        return result.OrderBy(s => s.Jid, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>mod_chat_stats lines: "direction peer kind count".</summary>
    private static List<XmppFederationTrafficResponse> ParseTraffic(string text)
    {
        var peers = new Dictionary<string, long[]>(StringComparer.OrdinalIgnoreCase); // [msgOut, msgIn, presOut, presIn, iqOut, iqIn]
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(' ');
            if (parts.Length != 4 || !long.TryParse(parts[3], out var count))
            {
                continue;
            }

            var counters = peers.TryGetValue(parts[1], out var existing) ? existing : peers[parts[1]] = new long[6];
            var outgoing = parts[0] == "out";
            var slot = parts[2] switch
            {
                "message" => outgoing ? 0 : 1,
                "presence" => outgoing ? 2 : 3,
                "iq" => outgoing ? 4 : 5,
                _ => -1
            };
            if (slot >= 0)
            {
                counters[slot] += count;
            }
        }

        return peers
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => new XmppFederationTrafficResponse(p.Key, p.Value[0], p.Value[1], p.Value[2], p.Value[3], p.Value[4], p.Value[5]))
            .ToList();
    }

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static long Number(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? AsLong(value) : 0;

    [GeneratedRegex(@"ejabberd\s+(\S+)\s+is running")]
    private static partial Regex VersionPattern();
}
