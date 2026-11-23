namespace CodingJamAPI.Options;

public sealed class XmppOptions
{
    public const string SectionName = "Xmpp";

    /// <summary>Jabber domain served by this chat server: accounts are username@Domain.</summary>
    public string Domain { get; init; } = "a.chat.test";

    /// <summary>Port Jabber clients connect to (published by docker compose); shown on the Settings page.</summary>
    public int ClientPort { get; init; } = 5222;

    /// <summary>Shared secret of the ejabberd external-auth bridge. Empty disables the internal auth endpoints.</summary>
    public string AuthSecret { get; init; } = string.Empty;

    /// <summary>Internal management API (mod_http_api / metrics) of this server's ejabberd.</summary>
    public string AdminApiUrl { get; init; } = "http://xmpp-a:5280";

    /// <summary>Optional second Jabber server used for federation (shown next to this one in the dashboard).</summary>
    public string PeerDomain { get; init; } = "b.chat.test";

    public string PeerAdminApiUrl { get; init; } = "http://xmpp-b:5280";
}
