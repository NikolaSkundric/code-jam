namespace CodingJamAPI.Domain.Common;

/// <summary>
/// Entity that is never physically removed by normal operations: delete sets IsDeleted/DeletedAt.
/// A global EF query filter (IsDeleted == false) should be applied to all derived types.
/// Exceptions where TASK.md requires permanent deletion (room deletion and its messages/files)
/// derive from BaseEntity or are hard-deleted explicitly via cascade from Chat.
/// </summary>
public abstract class SoftDeletableEntity : BaseEntity
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
