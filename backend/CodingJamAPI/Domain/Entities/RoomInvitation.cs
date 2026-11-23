using CodingJamAPI.Domain.Common;
using CodingJamAPI.Domain.Enums;

namespace CodingJamAPI.Domain.Entities;

public class RoomInvitation : SoftDeletableEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public Guid InvitedUserId { get; set; }
    public User InvitedUser { get; set; } = null!;

    public Guid InvitedById { get; set; }
    public User InvitedBy { get; set; } = null!;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
