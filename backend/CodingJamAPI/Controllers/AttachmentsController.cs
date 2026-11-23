using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace CodingJamAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/attachments")]
public class AttachmentsController(IAttachmentService attachments) : ControllerBase
{
    /// <summary>Downloads an attachment; only people who currently have access to its chat can.</summary>
    [HttpGet("{attachmentId:guid}")]
    public async Task<IActionResult> Download(Guid attachmentId, CancellationToken ct)
    {
        var file = await attachments.OpenAsync(User.GetUserId(), attachmentId, ct);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers[HeaderNames.CacheControl] = "private, max-age=3600";

        if (file.IsImage)
        {
            // Verified raster images are shown inline (img tags); a sandbox CSP keeps them inert if opened directly.
            Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
            var inline = new ContentDispositionHeaderValue("inline");
            inline.SetHttpFileName(file.FileName);
            Response.Headers[HeaderNames.ContentDisposition] = inline.ToString();
            return File(file.Content, file.ContentType);
        }

        // Everything else is always a download, never rendered by the browser.
        return File(file.Content, "application/octet-stream", file.FileName);
    }
}
