using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class PersonalChatConfiguration : IEntityTypeConfiguration<PersonalChat>
{
    public void Configure(EntityTypeBuilder<PersonalChat> builder)
    {
        builder.ToTable("PersonalChats");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Shared primary key with Chat.
        builder.HasOne(x => x.Chat)
            .WithOne(c => c.PersonalChat)
            .HasForeignKey<PersonalChat>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UserA)
            .WithMany()
            .HasForeignKey(x => x.UserAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UserB)
            .WithMany()
            .HasForeignKey(x => x.UserBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserAId, x.UserBId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(x => x.UserBId);
        builder.ToTable(t => t.HasCheckConstraint("CK_PersonalChats_Ordered", "\"UserAId\" < \"UserBId\""));
    }
}
