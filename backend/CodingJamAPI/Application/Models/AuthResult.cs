using CodingJamAPI.Contracts.Responses;

namespace CodingJamAPI.Application.Models;

public sealed record AuthResult(UserResponse User, string Token, DateTime ExpiresAt, bool Persistent);
