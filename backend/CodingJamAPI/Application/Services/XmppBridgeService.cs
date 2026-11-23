using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Domain.Exceptions;
using CodingJamAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodingJamAPI.Application.Services;

public sealed class XmppBridgeService(
    AppDbContext db,
    IPersonalChatService chats,
    IMessageService messages,
    IPresenceTracker tracker,
    IPresencePublisher publisher,
    IJabberGateway gateway,
    ILogger<XmppBridgeService> logger) : IXmppBridgeService
{
    public async Task IncomingMessageAsync(string fromUser, string toUser, string body, CancellationToken ct)
    {
        var from = await FindUserAsync(fromUser, ct);
        var to = await FindUserAsync(toUser, ct);
        if (from is null || to is null || from.Id == to.Id)
        {
            return;
        }

        try
        {
            // Opening the dialog only works for friends who have not banned each other (TASK.md 2.3.6).
            var conversation = await chats.OpenAsync(from.Id, to.Id, ct);
            await messages.ReceiveFromJabberAsync(from.Id, conversation.ChatId, body, ct);
        }
        catch (ValidationFailedException ex)
        {
            // Not friends, banned or too long: the message was still delivered by the Jabber server itself, it is just not
            // mirrored into the web chat.
            logger.LogDebug("Jabber message {From} -> {To} not mirrored: {Reason}", fromUser, toUser, ex.Message);
        }
    }

    public async Task PresenceAsync(string user, string resource, string ev, string? type, string? show, CancellationToken ct)
    {
        var found = await FindUserAsync(user, ct);
        if (found is null)
        {
            return;
        }

        var connectionId = ConnectionId(found.Id, resource);
        Application.Models.PresenceChange? change = null;

        switch (ev)
        {
            case "open":
                change = tracker.Connect(found.Id, found.Username, connectionId);
                break;
            case "close":
                change = tracker.Disconnect(connectionId);
                break;
            case "presence" when type == "unavailable":
                change = tracker.Disconnect(connectionId);
                break;
            case "presence":
                // A client that is "away", "extended away" or "do not disturb" counts as idle (AFK); otherwise active.
                change = tracker.Connect(found.Id, found.Username, connectionId);
                if (show is "away" or "xa" or "dnd")
                {
                    change = tracker.SetIdle(connectionId) ?? change;
                }
                break;
        }

        if (change is not null)
        {
            await publisher.PublishAsync(change, ct);
        }
    }

    public async Task SyncSessionsAsync(CancellationToken ct)
    {
        foreach (var (username, resource) in await gateway.GetSessionsAsync(ct))
        {
            await PresenceAsync(username, resource, "open", null, null, ct);
        }
    }

    private static string ConnectionId(Guid userId, string resource) => $"xmpp:{userId:N}/{resource}";

    private sealed record BridgedUser(Guid Id, string Username);

    private async Task<BridgedUser?> FindUserAsync(string username, CancellationToken ct)
    {
        var lower = username.ToLowerInvariant();
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Username.ToLower() == lower)
            .Select(u => new { u.Id, u.Username })
            .FirstOrDefaultAsync(ct);
        return user is null ? null : new BridgedUser(user.Id, user.Username);
    }
}
