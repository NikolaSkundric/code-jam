using CodingJamAPI.Domain.Common;
using CodingJamAPI.Domain.Enums;

namespace CodingJamAPI.Domain.Entities;

/// <summary>
/// Common conversation container for rooms and personal dialogs, so messages, attachments
/// and unread tracking work identically for both. Exactly one of Room / PersonalChat is set.
/// Hard deleted only together with its Room (TASK.md: room deletion is permanent).
/// </summary>
public class Chat : BaseEntity
{
    public ChatType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public Room? Room { get; set; }
    public PersonalChat? PersonalChat { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<ChatReadState> ReadStates { get; set; } = new List<ChatReadState>();
}
