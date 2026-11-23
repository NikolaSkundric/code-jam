using System.ComponentModel.DataAnnotations;
using CodingJamAPI.Domain.Enums;

namespace CodingJamAPI.Contracts.Requests;

public sealed class CreateRoomRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    public RoomVisibility Visibility { get; init; } = RoomVisibility.Public;
}

public sealed class UpdateRoomRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    public RoomVisibility Visibility { get; init; } = RoomVisibility.Public;
}

public sealed class InviteToRoomRequest
{
    [Required, StringLength(32, MinimumLength = 1)]
    public string Username { get; init; } = string.Empty;
}

public sealed class BanRoomMemberRequest
{
    [Required]
    public Guid UserId { get; init; }
}

public sealed class SetRoomRoleRequest
{
    /// <summary>True makes the member an admin, false removes admin status (the owner always stays admin).</summary>
    public bool IsAdmin { get; init; }
}
