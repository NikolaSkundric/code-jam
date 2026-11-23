using CodingJamAPI.Contracts.Responses;

namespace CodingJamAPI.Application.Abstractions;

/// <summary>Reads connection and federation statistics from the Jabber servers (ejabberd management API).</summary>
public interface IXmppAdminService
{
    Task<XmppDashboardResponse> GetDashboardAsync(CancellationToken ct);
}
