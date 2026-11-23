using CodingJamAPI.Domain.Common;
using CodingJamAPI.Domain.Enums;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Id equals Chat.Id (shared primary key). TASK.md requires room deletion to permanently delete
/// all messages, files and images, so a Room is hard deleted (cascading to its Chat, messages,
/// attachments, members, bans and invitations) and is intentionally not soft deletable.
/// </summary>
public class Room : BaseEntity
{
    public Chat Chat { get; set; } = null!;

    /// <summary>Unique.</summary>
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RoomVisibility Visibility { get; set; }

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    /// <summary>All members, including admins and the owner (see RoomMember.Role).</summary>
    public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
    public ICollection<RoomBan> Bans { get; set; } = new List<RoomBan>();
    public ICollection<RoomInvitation> Invitations { get; set; } = new List<RoomInvitation>();
}
