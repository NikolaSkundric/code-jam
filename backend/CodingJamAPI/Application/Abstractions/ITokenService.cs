namespace CodingJamAPI.Application.Abstractions;

public interface ITokenService
{
    /// <summary>
    /// Creates a signed JWT whose sub is the user id and jti is the session id.
    /// Persistent sessions (Keep me signed in) carry the claim rem=1 so sliding renewal keeps their lifetime.
    /// </summary>
    string CreateToken(Guid userId, Guid sessionId, DateTime expiresAtUtc, bool persistent);
}
