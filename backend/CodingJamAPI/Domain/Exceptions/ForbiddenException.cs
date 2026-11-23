namespace CodingJamAPI.Domain.Exceptions;

/// <summary>The caller may see the resource but is not allowed to do this (e.g. a read-only chat); mapped to HTTP 403.</summary>
public sealed class ForbiddenException(string message) : Exception(message);
