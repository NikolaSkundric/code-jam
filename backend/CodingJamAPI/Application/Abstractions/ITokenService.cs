namespace CodingJamAPI.Application.Abstractions;

public interface ITokenService
{
    /// <summary>Creates a signed JWT whose sub is the user id and jti is the session id.</summary>
    string CreateToken(Guid userId, Guid sessionId, DateTime expiresAtUtc);
}
