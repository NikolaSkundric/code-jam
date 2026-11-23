namespace CodingJamAPI.Domain.Common;

/// <summary>Every entity has a unique Guid primary key.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
