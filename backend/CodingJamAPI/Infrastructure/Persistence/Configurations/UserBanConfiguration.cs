using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class UserBanConfiguration : IEntityTypeConfiguration<UserBan>
{
    public void Configure(EntityTypeBuilder<UserBan> builder)
    {
        builder.ToTable("UserBans");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Banner)
            .WithMany(u => u.BansIssued)
            .HasForeignKey(x => x.BannerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BannedUser)
            .WithMany(u => u.BansReceived)
            .HasForeignKey(x => x.BannedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BannerId, x.BannedUserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.BannedUserId);
        builder.ToTable(t => t.HasCheckConstraint("CK_UserBans_NotSelf", "\"BannerId\" <> \"BannedUserId\""));
    }
}
