using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class ChatReadStateConfiguration : IEntityTypeConfiguration<ChatReadState>
{
    public void Configure(EntityTypeBuilder<ChatReadState> builder)
    {
        builder.ToTable("ChatReadStates");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Chat)
            .WithMany(c => c.ReadStates)
            .HasForeignKey(x => x.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ChatId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
