using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Controllers;

public sealed record XmppMessageEvent(string From, string To, string Body, string? Id);

public sealed record XmppPresenceEvent(string User, string Resource, string Event, string? Type, string? Show);

/// <summary>Events reported by xmpp/mod_chat_bridge: chat messages and presence of Jabber users.</summary>
public class XmppBridgeController(IXmppBridgeService bridge, IOptions<XmppOptions> options) : XmppInternalControllerBase(options)
{
    [HttpPost("message")]
    public async Task<IActionResult> Message(XmppMessageEvent message, CancellationToken ct)
    {
        if (!HasValidSecret()) return NotFound();
        await bridge.IncomingMessageAsync(message.From, message.To, message.Body, ct);
        return NoContent();
    }

    [HttpPost("presence")]
    public async Task<IActionResult> Presence(XmppPresenceEvent presence, CancellationToken ct)
    {
        if (!HasValidSecret()) return NotFound();
        await bridge.PresenceAsync(presence.User, presence.Resource, presence.Event, presence.Type, presence.Show, ct);
        return NoContent();
    }
}
