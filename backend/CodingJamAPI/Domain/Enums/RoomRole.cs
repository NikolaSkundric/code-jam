namespace CodingJamAPI.Domain.Enums;

/// <summary>Owner is always an admin and cannot lose admin privileges.</summary>
public enum RoomRole
{
    Member = 0,
    Admin = 1,
    Owner = 2
}
