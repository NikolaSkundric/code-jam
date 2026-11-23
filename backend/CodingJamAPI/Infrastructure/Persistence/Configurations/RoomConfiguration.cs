using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(20);

        // Shared primary key with Chat. Deleting the Chat row cascades to the room and all its content.
        builder.HasOne(x => x.Chat)
            .WithOne(c => c.Room)
            .HasForeignKey<Room>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Owner)
            .WithMany(u => u.OwnedRooms)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.OwnerId);
        builder.HasIndex(x => x.Visibility);
    }
}
