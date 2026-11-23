using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Domain.Enums;
using CodingJamAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodingJamAPI.Application.Services;

public sealed class ChatAccessService(AppDbContext db, IBanService bans) : IChatAccessService
{
    public async Task<ChatAccess?> GetAccessAsync(Guid userId, Guid chatId, CancellationToken ct)
    {
        var personal = await db.PersonalChats.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == chatId && (p.UserAId == userId || p.UserBId == userId), ct);

        if (personal is not null)
        {
            // Writable only while the two are friends and neither has banned the other (TASK.md 2.3.6).
            // Otherwise the history stays readable but frozen (2.3.5).
            var friends = await db.Friendships.AnyAsync(f => f.UserAId == personal.UserAId && f.UserBId == personal.UserBId, ct);
            var blocked = await bans.IsBlockedAsync(personal.UserAId, personal.UserBId, ct);
            return new ChatAccess(friends && !blocked, [personal.UserAId, personal.UserBId]);
        }

        // Rooms: only current members have access; a removed or banned user loses messages and files (TASK.md 2.4.8).
        var role = await db.RoomMembers.AsNoTracking()
            .Where(m => m.RoomId == chatId && m.UserId == userId)
            .Select(m => (RoomRole?)m.Role)
            .FirstOrDefaultAsync(ct);
        if (role is null)
        {
            return null;
        }

        var memberIds = await db.RoomMembers.AsNoTracking()
            .Where(m => m.RoomId == chatId)
            .Select(m => m.UserId)
            .ToListAsync(ct);
        return new ChatAccess(true, memberIds, CanModerate: role >= RoomRole.Admin);
    }
}
