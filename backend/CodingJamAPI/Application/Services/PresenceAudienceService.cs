using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodingJamAPI.Application.Services;

public sealed class PresenceAudienceService(AppDbContext db) : IPresenceAudienceService
{
    public async Task<HashSet<Guid>> GetVisibleUsersAsync(Guid userId, CancellationToken ct)
    {
        var friends = await db.Friendships.AsNoTracking()
            .Where(f => f.UserAId == userId || f.UserBId == userId)
            .Select(f => f.UserAId == userId ? f.UserBId : f.UserAId)
            .ToListAsync(ct);

        var myRooms = db.RoomMembers.Where(m => m.UserId == userId).Select(m => m.RoomId);
        var roommates = await db.RoomMembers.AsNoTracking()
            .Where(m => myRooms.Contains(m.RoomId))
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct);

        var visible = new HashSet<Guid>(friends);
        visible.UnionWith(roommates);
        visible.Remove(userId); // callers add the user themselves where needed
        return visible;
    }
}
