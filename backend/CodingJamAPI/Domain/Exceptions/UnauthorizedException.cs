namespace CodingJamAPI.Domain.Exceptions;

/// <summary>Authentication failed; mapped to HTTP 401.</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
