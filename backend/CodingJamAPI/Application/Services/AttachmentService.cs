using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Domain.Exceptions;
using CodingJamAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodingJamAPI.Application.Services;

public sealed class AttachmentService(AppDbContext db, IChatAccessService access, IFileStorage storage) : IAttachmentService
{
    public async Task<AttachmentDownload> OpenAsync(Guid userId, Guid attachmentId, CancellationToken ct)
    {
        var attachment = await db.Attachments.AsNoTracking()
            .Where(a => a.Id == attachmentId)
            .Select(a => new
            {
                a.FileName, a.ContentType, a.IsImage, a.SizeBytes, a.StoragePath,
                ChatId = db.Messages.Where(m => m.Id == a.MessageId).Select(m => m.ChatId).FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        // Same answer for "does not exist" and "no access": do not reveal which attachments exist.
        if (attachment is null || attachment.ChatId == Guid.Empty ||
            await access.GetAccessAsync(userId, attachment.ChatId, ct) is null)
        {
            throw new NotFoundException("Attachment not found.");
        }

        try
        {
            return new AttachmentDownload(
                storage.OpenRead(attachment.StoragePath), attachment.FileName, attachment.ContentType,
                attachment.IsImage, attachment.SizeBytes);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("Attachment not found.");
        }
    }
}
