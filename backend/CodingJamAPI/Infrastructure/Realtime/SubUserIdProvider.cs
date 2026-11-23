using Microsoft.AspNetCore.SignalR;

namespace CodingJamAPI.Infrastructure.Realtime;

/// <summary>SignalR user id = JWT "sub" claim (inbound claim mapping is disabled), so Clients.User(id) works.</summary>
public sealed class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.FindFirst("sub")?.Value;
}
