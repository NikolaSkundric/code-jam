using CodingJamAPI.Domain.Common;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Soft deleted on account removal. On deletion the service should also anonymize/free Email and
/// Username (or use unique indexes filtered by IsDeleted = false) and revoke all sessions.
/// </summary>
public class User : SoftDeletableEntity
{
    /// <summary>Unique.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Unique and immutable after registration.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>BCrypt hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public ICollection<Room> OwnedRooms { get; set; } = new List<Room>();
    public ICollection<RoomMember> RoomMemberships { get; set; } = new List<RoomMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public ICollection<FriendRequest> SentFriendRequests { get; set; } = new List<FriendRequest>();
    public ICollection<FriendRequest> ReceivedFriendRequests { get; set; } = new List<FriendRequest>();
    public ICollection<UserBan> BansIssued { get; set; } = new List<UserBan>();
    public ICollection<UserBan> BansReceived { get; set; } = new List<UserBan>();
}
