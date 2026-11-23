using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/chats/{chatId:guid}")]
public class MessagesController(IMessageService messages) : ControllerBase
{
    // 5 files x 20 MB plus form overhead; per-file limits are enforced by the service.
    private const long MaxUploadRequestBytes = 110L * 1024 * 1024;

    /// <summary>Newest messages, or the page older than <c>before</c> (message id) for infinite scroll.</summary>
    [HttpGet("messages")]
    public async Task<ActionResult<MessagePageResponse>> List(
        Guid chatId, [FromQuery] Guid? before, [FromQuery] int limit = 30, CancellationToken ct = default) =>
        Ok(await messages.GetMessagesAsync(User.GetUserId(), chatId, before, limit, ct));

    /// <summary>Text-only message (JSON).</summary>
    [HttpPost("messages")]
    [Consumes("application/json")]
    public async Task<ActionResult<MessageResponse>> Send(Guid chatId, SendMessageRequest request, CancellationToken ct)
    {
        var message = await messages.SendAsync(User.GetUserId(), chatId, request, [], ct);
        return StatusCode(StatusCodes.Status201Created, message);
    }

    /// <summary>Message with attachments (multipart/form-data: text, replyToMessageId, files[], comments[]).</summary>
    [HttpPost("messages")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
    public async Task<ActionResult<MessageResponse>> SendWithFiles(
        Guid chatId, [FromForm] SendMessageFormRequest form, CancellationToken ct)
    {
        var files = form.Files
            .Select((file, index) => new UploadedFile(
                file.FileName, file.ContentType, file.Length, file.OpenReadStream,
                index < form.Comments.Count ? form.Comments[index] : null))
            .ToList();

        var request = new SendMessageRequest { Text = form.Text, ReplyToMessageId = form.ReplyToMessageId };
        var message = await messages.SendAsync(User.GetUserId(), chatId, request, files, ct);
        return StatusCode(StatusCodes.Status201Created, message);
    }

    [HttpPut("messages/{messageId:guid}")]
    public async Task<ActionResult<MessageResponse>> Edit(
        Guid chatId, Guid messageId, EditMessageRequest request, CancellationToken ct) =>
        Ok(await messages.EditAsync(User.GetUserId(), chatId, messageId, request, ct));

    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> Delete(Guid chatId, Guid messageId, CancellationToken ct)
    {
        await messages.DeleteAsync(User.GetUserId(), chatId, messageId, ct);
        return NoContent();
    }

    /// <summary>Clears the unread indicator for this chat.</summary>
    [HttpPost("read")]
    public async Task<IActionResult> MarkRead(Guid chatId, CancellationToken ct)
    {
        await messages.MarkReadAsync(User.GetUserId(), chatId, ct);
        return NoContent();
    }
}
