using CodingJamAPI.Application.Abstractions;
using CodingJamAPI.Application.Models;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;
using CodingJamAPI.Extensions;
using CodingJamAPI.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodingJamAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, IAuthCookieService cookies) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(request, GetClientInfo(), ct);
        cookies.Append(Response, result.Token, result.ExpiresAt, result.Persistent);
        return StatusCode(StatusCodes.Status201Created, result.User);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, GetClientInfo(), ct);
        cookies.Append(Response, result.Token, result.ExpiresAt, result.Persistent);
        return Ok(result.User);
    }

    /// <summary>Signs out the current browser session only.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(User.GetSessionId(), ct);
        cookies.Delete(Response);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct) =>
        Ok(await auth.GetCurrentUserAsync(User.GetUserId(), ct));

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await auth.ForgotPasswordAsync(request, ct);
        return Accepted(); // Same response whether or not the email exists.
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await auth.ResetPasswordAsync(request, ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(User.GetUserId(), User.GetSessionId(), request, ct);
        return NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> Sessions(CancellationToken ct) =>
        Ok(await auth.GetSessionsAsync(User.GetUserId(), User.GetSessionId(), ct));

    /// <summary>Logs out one of the user's sessions (possibly the current one).</summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        await auth.RevokeSessionAsync(User.GetUserId(), sessionId, ct);
        if (sessionId == User.GetSessionId())
        {
            cookies.Delete(Response);
        }
        return NoContent();
    }

    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request, CancellationToken ct)
    {
        await auth.DeleteAccountAsync(User.GetUserId(), request, ct);
        cookies.Delete(Response);
        return NoContent();
    }

    private ClientInfo GetClientInfo() => new(
        Request.Headers.UserAgent.ToString(),
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
}
