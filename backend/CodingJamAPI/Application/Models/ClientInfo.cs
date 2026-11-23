namespace CodingJamAPI.Application.Models;

/// <summary>Browser details recorded with a session (shown in the active sessions list).</summary>
public sealed record ClientInfo(string UserAgent, string IpAddress);
