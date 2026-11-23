using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/personal-chats")]
public class PersonalChatsController(IPersonalChatService chats) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationResponse>>> List(CancellationToken ct) =>
        Ok(await chats.GetConversationsAsync(User.GetUserId(), ct));

    /// <summary>Opens (or creates) the dialog with a friend.</summary>
    [HttpPost]
    public async Task<ActionResult<ConversationResponse>> Open(OpenPersonalChatRequest request, CancellationToken ct) =>
        Ok(await chats.OpenAsync(User.GetUserId(), request.UserId, ct));
}
