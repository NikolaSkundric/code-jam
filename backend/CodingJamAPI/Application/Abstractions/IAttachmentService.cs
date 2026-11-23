using CodingJamAPI.Application.Models;

namespace CodingJamAPI.Application.Abstractions;

public interface IAttachmentService
{
    /// <summary>Opens an attachment for a user who currently has access to its chat; otherwise NotFound.</summary>
    Task<AttachmentDownload> OpenAsync(Guid userId, Guid attachmentId, CancellationToken ct);
}
