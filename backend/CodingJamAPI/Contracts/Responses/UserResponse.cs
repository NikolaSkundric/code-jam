namespace CodingJamAPI.Contracts.Responses;

/// <summary>IsAdmin: the user may open the administration pages (Admin:Usernames setting).</summary>
public sealed record UserResponse(Guid Id, string Email, string Username, bool IsAdmin = false);
