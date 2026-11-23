using CodingJamAPI.Application.Models;

namespace CodingJamAPI.Application.Abstractions;

/// <summary>Tells the people who may see a user's presence (friends and room mates) that the status changed.</summary>
public interface IPresencePublisher
{
    Task PublishAsync(PresenceChange change, CancellationToken ct = default);
}
