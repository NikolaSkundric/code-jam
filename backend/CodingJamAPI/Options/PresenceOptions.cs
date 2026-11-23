namespace CodingJamAPI.Options;

public sealed class PresenceOptions
{
    public const string SectionName = "Presence";

    /// <summary>A connection (browser tab) with no activity for longer than this is idle (TASK.md: 1 minute).</summary>
    public int AfkAfterSeconds { get; init; } = 60;

    /// <summary>How often idle tabs are detected. Keeps AFK updates well within the 2 second target.</summary>
    public int SweepIntervalMilliseconds { get; init; } = 1000;
}
