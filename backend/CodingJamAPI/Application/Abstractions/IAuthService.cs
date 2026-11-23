using CodingJamAPI.Application.Models;
using CodingJamAPI.Contracts.Requests;
using CodingJamAPI.Contracts.Responses;

namespace CodingJamAPI.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, ClientInfo client, CancellationToken ct);
    Task<AuthResult> LoginAsync(LoginRequest request, ClientInfo client, CancellationToken ct);
    Task LogoutAsync(Guid sessionId, CancellationToken ct);
    Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct);

    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
    Task ChangePasswordAsync(Guid userId, Guid currentSessionId, ChangePasswordRequest request, CancellationToken ct);

    Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(Guid userId, Guid currentSessionId, CancellationToken ct);
    Task RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken ct);

    Task DeleteAccountAsync(Guid userId, DeleteAccountRequest request, CancellationToken ct);
}
