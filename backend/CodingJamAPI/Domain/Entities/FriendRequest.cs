using CodingJamAPI.Domain.Common;
using CodingJamAPI.Domain.Enums;

namespace CodingJamAPI.Domain.Entities;

public class FriendRequest : SoftDeletableEntity
{
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public Guid RecipientId { get; set; }
    public User Recipient { get; set; } = null!;

    /// <summary>Optional text sent with the request.</summary>
    public string? Message { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
