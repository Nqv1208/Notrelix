using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities;

namespace Notrelix.Infrastructure.Data.Configurations;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("reactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Emoji).HasColumnName("emoji").HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => new { x.ResourceType, x.ResourceId, x.UserId, x.Emoji }).IsUnique();
        builder.Ignore(x => x.DomainEvents);
    }
}
