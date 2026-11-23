namespace CodingJamAPI.Contracts.Responses;

/// <summary>One connected Jabber client session.</summary>
public sealed record XmppSessionResponse(
    string Jid,
    string Connection,
    string Ip,
    int Port,
    int Priority,
    long UptimeSeconds,
    string Status);

/// <summary>Stanzas exchanged with one remote server (federation traffic since the server started).</summary>
public sealed record XmppFederationTrafficResponse(
    string Peer,
    long MessagesOut,
    long MessagesIn,
    long PresenceOut,
    long PresenceIn,
    long IqOut,
    long IqIn);

public sealed record XmppServerResponse(
    string Name,
    string Domain,
    bool Available,
    string? Error,
    string? Version,
    long UptimeSeconds,
    int ConnectedUsers,
    int IncomingServerConnections,
    int OutgoingServerConnections,
    IReadOnlyList<XmppSessionResponse> Sessions,
    IReadOnlyList<XmppFederationTrafficResponse> Traffic);

/// <summary>Admin dashboard: the Jabber server of this chat and the federated peer server.</summary>
public sealed record XmppDashboardResponse(XmppServerResponse Local, XmppServerResponse? Peer, DateTime GeneratedAt);

/// <summary>What a user needs to connect with a Jabber client.</summary>
public sealed record JabberInfoResponse(string Domain, int Port);
