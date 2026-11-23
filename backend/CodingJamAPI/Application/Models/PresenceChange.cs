using CodingJamAPI.Domain.Enums;

namespace CodingJamAPI.Application.Models;

/// <summary>A user presence status, sent to clients when it changes.</summary>
public sealed record PresenceChange(Guid UserId, string Username, PresenceStatus Status);
