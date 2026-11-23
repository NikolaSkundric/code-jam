using CodingJamAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodingJamAPI.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.Username).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(100);

        // Unique among active accounts; a deleted account frees its email/username.
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.Username).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
