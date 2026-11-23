using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(x => x.Id);

        // Max 3 KB of UTF-8 text; the exact byte limit is enforced in validation.
        builder.Property(x => x.Text).IsRequired().HasMaxLength(3072);

        builder.HasOne(x => x.Chat)
            .WithMany(c => c.Messages)
            .HasForeignKey(x => x.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany(u => u.Messages)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReplyToMessage)
            .WithMany()
            .HasForeignKey(x => x.ReplyToMessageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Keyset pagination for infinite scroll: newest first within a chat.
        builder.HasIndex(x => new { x.ChatId, x.CreatedAt, x.Id });
        builder.HasIndex(x => x.AuthorId);
        builder.HasIndex(x => x.ReplyToMessageId);
    }
}
