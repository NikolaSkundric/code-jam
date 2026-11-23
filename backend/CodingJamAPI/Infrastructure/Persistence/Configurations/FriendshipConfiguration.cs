using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("Friendships");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.UserA)
            .WithMany()
            .HasForeignKey(x => x.UserAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UserB)
            .WithMany()
            .HasForeignKey(x => x.UserBId)
            .OnDelete(DeleteBehavior.Restrict);

        // Canonical ordering (UserAId < UserBId) makes each pair unique.
        builder.HasIndex(x => new { x.UserAId, x.UserBId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.UserBId);
        builder.ToTable(t => t.HasCheckConstraint("CK_Friendships_Ordered", "\"UserAId\" < \"UserBId\""));
    }
}
