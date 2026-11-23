namespace CodingJamAPI.Application.Abstractions;

/// <summary>
/// Commands to our Jabber server (A) from the web side: deliver chat messages to Jabber clients and keep the
/// Jabber rosters in line with the friends lists. Every method is best effort: failures are logged, never thrown.
/// </summary>
public interface IJabberGateway
{
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Delivers a chat message from one web user to another as a Jabber message.</summary>
    Task SendChatMessageAsync(string fromUsername, string toUsername, string body);

    /// <summary>Puts both users in each other's Jabber roster (subscription both).</summary>
    Task AddFriendshipAsync(string usernameA, string usernameB);

    Task RemoveFriendshipAsync(string usernameA, string usernameB);

    /// <summary>Disconnects every Jabber session of a user (deleted account).</summary>
    Task KickUserAsync(string username);

    /// <summary>Currently connected Jabber sessions as (username, resource).</summary>
    Task<IReadOnlyList<(string Username, string Resource)>> GetSessionsAsync(CancellationToken ct = default);
}
