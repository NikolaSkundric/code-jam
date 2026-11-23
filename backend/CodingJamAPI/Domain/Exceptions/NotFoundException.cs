namespace CodingJamAPI.Domain.Exceptions;

/// <summary>Resource not found (or not visible to the caller); mapped to HTTP 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
