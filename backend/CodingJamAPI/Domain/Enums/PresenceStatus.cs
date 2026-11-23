namespace CodingJamAPI.Domain.Enums;

/// <summary>Computed at runtime from open tabs/connections; never persisted.</summary>
public enum PresenceStatus
{
    Offline = 0,
    Afk = 1,
    Online = 2
}
