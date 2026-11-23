using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class RoomBanConfiguration : IEntityTypeConfiguration<RoomBan>
{
    public void Configure(EntityTypeBuilder<RoomBan> builder)
    {
        builder.ToTable("RoomBans");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Room)
            .WithMany(r => r.Bans)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BannedBy)
            .WithMany()
            .HasForeignKey(x => x.BannedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RoomId, x.UserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.UserId);
    }
}
