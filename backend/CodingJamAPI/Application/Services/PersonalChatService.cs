using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Domain.Common;
using CodingJamAPI.Domain.Entities;
using CodingJamAPI.Domain.Enums;
using CodingJamAPI.Domain.Exceptions;
using CodingJamAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CodingJamAPI.Application.Services;

public sealed class PersonalChatService(AppDbContext db, IBanService bans) : IPersonalChatService
{
    private const int PreviewLength = 120;

    public async Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(Guid userId, CancellationToken ct)
    {
        var chats = await db.PersonalChats.AsNoTracking()
            .Where(p => p.UserAId == userId || p.UserBId == userId)
            .ToListAsync(ct);
        if (chats.Count == 0)
        {
            return [];
        }

        var otherIds = chats.Select(c => OtherUser(c, userId)).ToList();
        var names = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => otherIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);

        var friendIds = (await db.Friendships.AsNoTracking()
                .Where(f => f.UserAId == userId || f.UserBId == userId)
                .Select(f => f.UserAId == userId ? f.UserBId : f.UserAId)
                .ToListAsync(ct)).ToHashSet();
        var blockedIds = (await db.UserBans.AsNoTracking()
                .Where(b => b.BannerId == userId || b.BannedUserId == userId)
                .Select(b => b.BannerId == userId ? b.BannedUserId : b.BannerId)
                .ToListAsync(ct)).ToHashSet();

        var result = new List<ConversationResponse>();
        foreach (var chat in chats)
        {
            var otherId = OtherUser(chat, userId);
            if (!names.TryGetValue(otherId, out var username))
            {
                continue;
            }
            result.Add(await BuildAsync(userId, chat.Id, otherId, username,
                canWrite: friendIds.Contains(otherId) && !blockedIds.Contains(otherId), ct));
        }

        return result
            .OrderByDescending(c => c.LastMessageAt ?? DateTime.MinValue)
            .ThenBy(c => c.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<ConversationResponse> OpenAsync(Guid userId, Guid friendId, CancellationToken ct)
    {
        var (a, b) = GuidPair.Order(userId, friendId);
        var isFriend = userId != friendId && await db.Friendships.AnyAsync(f => f.UserAId == a && f.UserBId == b, ct);
        if (!isFriend || await bans.IsBlockedAsync(userId, friendId, ct))
        {
            throw new ValidationFailedException("userId", "You can only chat with your friends.");
        }

        var username = await db.Users.AsNoTracking().Where(u => u.Id == friendId).Select(u => u.Username).FirstAsync(ct);

        var chat = await db.PersonalChats.AsNoTracking().FirstOrDefaultAsync(p => p.UserAId == a && p.UserBId == b, ct);
        if (chat is null)
        {
            var chatId = Guid.NewGuid();
            db.PersonalChats.Add(new PersonalChat
            {
                Id = chatId,
                UserAId = a,
                UserBId = b,
                Chat = new Chat { Id = chatId, Type = ChatType.Personal, CreatedAt = DateTime.UtcNow }
            });

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Created concurrently by the other user.
                db.ChangeTracker.Clear();
            }

            chat = await db.PersonalChats.AsNoTracking().FirstAsync(p => p.UserAId == a && p.UserBId == b, ct);
        }

        return await BuildAsync(userId, chat.Id, friendId, username, canWrite: true, ct);
    }

    private async Task<ConversationResponse> BuildAsync(
        Guid userId, Guid chatId, Guid otherId, string username, bool canWrite, CancellationToken ct)
    {
        var last = await db.Messages.AsNoTracking()
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Select(m => new { m.Text, m.CreatedAt })
            .FirstOrDefaultAsync(ct);

        var lastRead = await db.ChatReadStates.AsNoTracking()
            .Where(r => r.ChatId == chatId && r.UserId == userId)
            .Select(r => (DateTime?)r.LastReadAt)
            .FirstOrDefaultAsync(ct) ?? DateTime.MinValue;

        var unread = await db.Messages.CountAsync(
            m => m.ChatId == chatId && m.AuthorId != userId && m.CreatedAt > lastRead, ct);

        var preview = last is null ? null
            : last.Text.Length == 0 ? "📎 Attachment"
            : last.Text.Length <= PreviewLength ? last.Text : last.Text[..PreviewLength];
        return new ConversationResponse(chatId, otherId, username, preview, last?.CreatedAt, unread, canWrite);
    }

    private static Guid OtherUser(PersonalChat chat, Guid userId) => chat.UserAId == userId ? chat.UserBId : chat.UserAId;
}
