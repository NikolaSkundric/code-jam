using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("FriendRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(x => x.Sender)
            .WithMany(u => u.SentFriendRequests)
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Recipient)
            .WithMany(u => u.ReceivedFriendRequests)
            .HasForeignKey(x => x.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Only one pending request per direction.
        builder.HasIndex(x => new { x.SenderId, x.RecipientId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false AND \"Status\" = 'Pending'");

        builder.HasIndex(x => x.RecipientId);
        builder.ToTable(t => t.HasCheckConstraint("CK_FriendRequests_NotSelf", "\"SenderId\" <> \"RecipientId\""));
    }
}
