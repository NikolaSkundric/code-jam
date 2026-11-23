using System.Text;
using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Domain.Entities;
using CodingJamAPI.Domain.Exceptions;
using CodingJamAPI.Infrastructure.Persistence;
using CodingJamAPI.Infrastructure.Realtime;
using CodingJamAPI.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodingJamAPI.Application.Services;

public sealed class MessageService(
    AppDbContext db,
    IChatAccessService access,
    IRealtimeNotifier notifier,
    IFileStorage storage,
    IOptions<StorageOptions> storageOptions,
    IJabberGateway jabber) : IMessageService
{
    /// <summary>TASK.md 2.5.2: maximum text size per message.</summary>
    private const int MaxTextBytes = 3 * 1024;
    private const int MaxCommentLength = 1000;
    private const int MaxFileNameLength = 255;
    private const int MaxPageSize = 100;
    private const int ReplyPreviewLength = 200;

    private readonly StorageOptions _storage = storageOptions.Value;

    public async Task<MessagePageResponse> GetMessagesAsync(
        Guid userId, Guid chatId, Guid? before, int limit, CancellationToken ct)
    {
        await RequireAccessAsync(userId, chatId, ct);
        limit = Math.Clamp(limit, 1, MaxPageSize);

        var query = db.Messages.AsNoTracking().Where(m => m.ChatId == chatId);
        if (before is not null)
        {
            var pivot = await db.Messages.IgnoreQueryFilters().AsNoTracking()
                .Where(m => m.Id == before && m.ChatId == chatId)
                .Select(m => new { m.CreatedAt, m.Id })
                .FirstOrDefaultAsync(ct);
            if (pivot is not null)
            {
                query = query.Where(m => m.CreatedAt < pivot.CreatedAt ||
                                         (m.CreatedAt == pivot.CreatedAt && m.Id.CompareTo(pivot.Id) < 0));
            }
        }

        var page = await query
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = page.Count > limit;
        var items = page.Take(limit).Reverse().ToList();
        return new MessagePageResponse(await MapAsync(items, ct), hasMore);
    }

    public async Task<MessageResponse> SendAsync(
        Guid userId, Guid chatId, SendMessageRequest request, IReadOnlyList<UploadedFile> files, CancellationToken ct)
    {
        var chatAccess = await RequireWriteAsync(userId, chatId, ct);
        var text = ValidateText(request.Text, allowEmpty: files.Count > 0);
        var prepared = PrepareFiles(files);

        if (request.ReplyToMessageId is { } replyId &&
            !await db.Messages.AnyAsync(m => m.Id == replyId && m.ChatId == chatId, ct))
        {
            throw new ValidationFailedException("replyToMessageId", "The message you are replying to no longer exists.");
        }

        var message = new Message
        {
            ChatId = chatId,
            AuthorId = userId,
            Text = text,
            ReplyToMessageId = request.ReplyToMessageId,
            CreatedAt = DateTime.UtcNow
        };
        db.Messages.Add(message);

        // Write the files first; if anything fails afterwards, remove them again.
        var savedKeys = new List<string>();
        try
        {
            for (var index = 0; index < prepared.Count; index++)
            {
                var file = prepared[index];
                var attachmentId = Guid.NewGuid();
                var key = $"{chatId:N}/{attachmentId:N}";
                await using (var stream = file.Source.Open())
                {
                    await storage.SaveAsync(key, stream, ct);
                }
                savedKeys.Add(key);

                db.Attachments.Add(new Attachment
                {
                    Id = attachmentId,
                    Message = message,
                    UploadedById = userId,
                    FileName = file.FileName,
                    StoragePath = key,
                    ContentType = file.ContentType,
                    SizeBytes = file.Source.Length,
                    IsImage = file.IsImage,
                    Comment = file.Comment,
                    // One microsecond apart so the upload order is preserved when listing.
                    CreatedAt = message.CreatedAt.AddTicks(index * 10)
                });
            }

            await db.SaveChangesAsync(ct);
        }
        catch
        {
            foreach (var key in savedKeys)
            {
                storage.Delete(key);
            }
            throw;
        }

        var response = (await MapAsync([message], ct))[0];
        await notifier.NotifyAsync(chatAccess.ParticipantIds, RealtimeEvents.MessageReceived, response, ct);
        await ForwardToJabberAsync(userId, chatId, text, prepared, chatAccess, ct);
        return response;
    }

    public async Task<MessageResponse> ReceiveFromJabberAsync(Guid authorId, Guid chatId, string text, CancellationToken ct)
    {
        var chatAccess = await RequireWriteAsync(authorId, chatId, ct);
        var message = new Message
        {
            ChatId = chatId,
            AuthorId = authorId,
            Text = ValidateText(text, allowEmpty: false),
            CreatedAt = DateTime.UtcNow
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);

        var response = (await MapAsync([message], ct))[0];
        await notifier.NotifyAsync(chatAccess.ParticipantIds, RealtimeEvents.MessageReceived, response, ct);
        return response;
    }

    public async Task<MessageResponse> EditAsync(
        Guid userId, Guid chatId, Guid messageId, EditMessageRequest request, CancellationToken ct)
    {
        var chatAccess = await RequireWriteAsync(userId, chatId, ct);
        var message = await FindOwnMessageAsync(userId, chatId, messageId, ct);

        // A message that carries attachments may have its text cleared.
        var hasAttachments = await db.Attachments.AnyAsync(a => a.MessageId == messageId, ct);
        message.Text = ValidateText(request.Text, allowEmpty: hasAttachments);
        message.EditedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var response = (await MapAsync([message], ct))[0];
        await notifier.NotifyAsync(chatAccess.ParticipantIds, RealtimeEvents.MessageEdited, response, ct);
        return response;
    }

    public async Task DeleteAsync(Guid userId, Guid chatId, Guid messageId, CancellationToken ct)
    {
        var chatAccess = await RequireWriteAsync(userId, chatId, ct);
        var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId, ct)
                      ?? throw new NotFoundException("Message not found.");

        // The author may delete their message; room owner and admins may delete any message (TASK.md 2.5.5, 2.4.7).
        if (message.AuthorId != userId && !chatAccess.CanModerate)
        {
            throw new ForbiddenException("You can only delete your own messages.");
        }

        // Soft delete the message and its attachments (the files stay on disk until the chat is deleted).
        db.Messages.Remove(message);
        db.Attachments.RemoveRange(await db.Attachments.Where(a => a.MessageId == messageId).ToListAsync(ct));
        await db.SaveChangesAsync(ct);

        await notifier.NotifyAsync(chatAccess.ParticipantIds, RealtimeEvents.MessageDeleted,
            new MessageDeletedEvent(chatId, messageId), ct);
    }

    public async Task MarkReadAsync(Guid userId, Guid chatId, CancellationToken ct)
    {
        await RequireAccessAsync(userId, chatId, ct);
        var now = DateTime.UtcNow;

        var updated = await db.ChatReadStates
            .Where(r => r.ChatId == chatId && r.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastReadAt, now), ct);
        if (updated == 0)
        {
            db.ChatReadStates.Add(new ChatReadState { ChatId = chatId, UserId = userId, LastReadAt = now });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Created concurrently by another tab: update it instead.
                db.ChangeTracker.Clear();
                await db.ChatReadStates.Where(r => r.ChatId == chatId && r.UserId == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastReadAt, now), ct);
            }
        }

        // Other tabs of this user clear their unread indicator too.
        await notifier.NotifyAsync([userId], RealtimeEvents.ConversationsChanged, null, ct);
    }

    // ---------- helpers ----------

    /// <summary>
    /// A message written on the website in a personal chat is also delivered to the Jabber clients of the other
    /// person. Rooms are not bridged.
    /// </summary>
    private async Task ForwardToJabberAsync(
        Guid authorId, Guid chatId, string text, IReadOnlyList<PreparedFile> files, ChatAccess chatAccess, CancellationToken ct)
    {
        if (chatAccess.ParticipantIds.Count != 2 || !await db.PersonalChats.AnyAsync(p => p.Id == chatId, ct))
        {
            return;
        }

        var otherId = chatAccess.ParticipantIds.First(id => id != authorId);
        var names = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Id == authorId || u.Id == otherId)
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);
        if (!names.TryGetValue(authorId, out var from) || !names.TryGetValue(otherId, out var to))
        {
            return;
        }

        var lines = new List<string>();
        if (text.Length > 0) lines.Add(text);
        lines.AddRange(files.Select(f => $"[attachment: {f.FileName}]"));
        _ = jabber.SendChatMessageAsync(from, to, string.Join("\n", lines)); // best effort, not awaited
    }

    private sealed record PreparedFile(UploadedFile Source, string FileName, string ContentType, bool IsImage, string? Comment);

    /// <summary>Validates every file (size limits per TASK.md 3.4, real image detection) before anything is stored.</summary>
    private List<PreparedFile> PrepareFiles(IReadOnlyList<UploadedFile> files)
    {
        if (files.Count > _storage.MaxFilesPerMessage)
        {
            throw new ValidationFailedException("files", $"You can attach at most {_storage.MaxFilesPerMessage} files to a message.");
        }

        var prepared = new List<PreparedFile>();
        foreach (var file in files)
        {
            var name = SanitizeFileName(file.FileName);
            if (file.Length <= 0)
            {
                throw new ValidationFailedException("files", $"{name} is empty.");
            }

            var head = new byte[12];
            int read;
            using (var stream = file.Open())
            {
                read = stream.Read(head, 0, head.Length);
            }
            var imageType = FileTypeSniffer.DetectImage(head.AsSpan(0, read));
            var isImage = imageType is not null;

            if (isImage && file.Length > _storage.MaxImageBytes)
            {
                throw new ValidationFailedException("files", $"{name} is too large (images: maximum {_storage.MaxImageBytes / 1024 / 1024} MB).");
            }
            if (file.Length > _storage.MaxFileBytes)
            {
                throw new ValidationFailedException("files", $"{name} is too large (files: maximum {_storage.MaxFileBytes / 1024 / 1024} MB).");
            }

            var comment = string.IsNullOrWhiteSpace(file.Comment) ? null : file.Comment.Trim();
            if (comment is { Length: > MaxCommentLength })
            {
                throw new ValidationFailedException("files", $"The comment for {name} is too long.");
            }

            var contentType = imageType ?? (string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            prepared.Add(new PreparedFile(file, name, contentType.Length > 255 ? "application/octet-stream" : contentType, isImage, comment));
        }
        return prepared;
    }

    /// <summary>Keeps the original name for display and download but drops any path and control characters.</summary>
    private static string SanitizeFileName(string raw)
    {
        var name = Path.GetFileName(raw.Replace('\\', '/'));
        name = new string(name.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (name.Length > MaxFileNameLength)
        {
            var ext = Path.GetExtension(name);
            name = name[..(MaxFileNameLength - ext.Length)] + ext;
        }
        return name.Length == 0 ? "file" : name;
    }

    private async Task<ChatAccess> RequireAccessAsync(Guid userId, Guid chatId, CancellationToken ct) =>
        await access.GetAccessAsync(userId, chatId, ct) ?? throw new NotFoundException("Chat not found.");

    private async Task<ChatAccess> RequireWriteAsync(Guid userId, Guid chatId, CancellationToken ct)
    {
        var chatAccess = await RequireAccessAsync(userId, chatId, ct);
        if (!chatAccess.CanWrite)
        {
            throw new ForbiddenException("This chat is read-only.");
        }
        return chatAccess;
    }

    private async Task<Message> FindOwnMessageAsync(Guid userId, Guid chatId, Guid messageId, CancellationToken ct)
    {
        var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m.ChatId == chatId, ct)
                      ?? throw new NotFoundException("Message not found.");
        if (message.AuthorId != userId)
        {
            throw new ForbiddenException("You can only change your own messages.");
        }
        return message;
    }

    private static string ValidateText(string? raw, bool allowEmpty)
    {
        // Keep inner whitespace/newlines (multiline text), but reject blank messages without attachments.
        var text = (raw ?? string.Empty).TrimEnd();
        if (string.IsNullOrWhiteSpace(text))
        {
            return allowEmpty ? string.Empty : throw new ValidationFailedException("text", "Message cannot be empty.");
        }
        if (Encoding.UTF8.GetByteCount(text) > MaxTextBytes)
        {
            throw new ValidationFailedException("text", "Message is too long (maximum 3 KB).");
        }
        return text;
    }

    private async Task<IReadOnlyList<MessageResponse>> MapAsync(IReadOnlyList<Message> messages, CancellationToken ct)
    {
        if (messages.Count == 0)
        {
            return [];
        }

        // Deleted replied-to messages are still looked up (marked as deleted, text hidden).
        var replyIds = messages.Where(m => m.ReplyToMessageId != null).Select(m => m.ReplyToMessageId!.Value).Distinct().ToList();
        var replies = replyIds.Count == 0
            ? new Dictionary<Guid, (Guid AuthorId, string Text, bool Deleted)>()
            : await db.Messages.IgnoreQueryFilters().AsNoTracking()
                .Where(m => replyIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => (AuthorId: m.AuthorId, Text: m.Text, Deleted: m.IsDeleted), ct);

        // Authors may be deleted accounts; their username is kept.
        var userIds = messages.Select(m => m.AuthorId).Concat(replies.Values.Select(r => r.AuthorId)).Distinct().ToList();
        var names = await db.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username, ct);

        var messageIds = messages.Select(m => m.Id).ToList();
        var attachments = (await db.Attachments.AsNoTracking()
                .Where(a => messageIds.Contains(a.MessageId))
                .OrderBy(a => a.CreatedAt).ThenBy(a => a.FileName)
                .ToListAsync(ct))
            .GroupBy(a => a.MessageId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AttachmentResponse>)g
                .Select(a => new AttachmentResponse(a.Id, a.FileName, a.ContentType, a.SizeBytes, a.IsImage, a.Comment))
                .ToList());

        string NameOf(Guid id) => names.TryGetValue(id, out var n) ? n : "unknown";

        return messages.Select(m =>
        {
            ReplyPreviewResponse? reply = null;
            if (m.ReplyToMessageId is { } rid && replies.TryGetValue(rid, out var r))
            {
                var preview = r.Text.Length <= ReplyPreviewLength ? r.Text : r.Text[..ReplyPreviewLength];
                reply = new ReplyPreviewResponse(rid, NameOf(r.AuthorId), r.Deleted ? null : preview, r.Deleted);
            }
            return new MessageResponse(
                m.Id, m.ChatId, m.AuthorId, NameOf(m.AuthorId), m.Text, reply, m.CreatedAt, m.EditedAt,
                attachments.TryGetValue(m.Id, out var list) ? list : []);
        }).ToList();
    }
}
