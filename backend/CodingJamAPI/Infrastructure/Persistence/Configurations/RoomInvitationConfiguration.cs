using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class RoomInvitationConfiguration : IEntityTypeConfiguration<RoomInvitation>
{
    public void Configure(EntityTypeBuilder<RoomInvitation> builder)
    {
        builder.ToTable("RoomInvitations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(x => x.Room)
            .WithMany(r => r.Invitations)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.InvitedUser)
            .WithMany()
            .HasForeignKey(x => x.InvitedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InvitedBy)
            .WithMany()
            .HasForeignKey(x => x.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RoomId, x.InvitedUserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false AND \"Status\" = 'Pending'");

        builder.HasIndex(x => x.InvitedUserId);
    }
}
