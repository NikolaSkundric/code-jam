namespace CodingJamAPI.Application.Abstractions;

/// <summary>What the Jabber server reports about its users (see xmpp/mod_chat_bridge): messages and presence.</summary>
public interface IXmppBridgeService
{
    /// <summary>A Jabber user wrote to another Jabber user: mirror it into their web personal chat (friends only).</summary>
    Task IncomingMessageAsync(string fromUser, string toUser, string body, CancellationToken ct);

    /// <summary>A Jabber session opened, closed or changed its presence: reflect it in the website presence.</summary>
    Task PresenceAsync(string user, string resource, string ev, string? type, string? show, CancellationToken ct);

    /// <summary>Registers the Jabber sessions that exist right now (after the API restarted).</summary>
    Task SyncSessionsAsync(CancellationToken ct);
}
