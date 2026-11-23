using CodingJamAPI.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CodingJamAPI.Infrastructure.Persistence;

/// <summary>
/// Turns Remove() on a SoftDeletableEntity into an update (IsDeleted = true, DeletedAt = now).
/// Entities that derive only from BaseEntity (Room, Chat, ...) are physically deleted.
/// Note: permanent room deletion should delete the Chat row with ExecuteDeleteAsync so that the
/// database cascade removes its messages and attachments instead of soft deleting tracked rows.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<SoftDeletableEntity>()
                     .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
        }
    }
}
