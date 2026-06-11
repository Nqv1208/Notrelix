using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Reactions;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("reactions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").IsRequired().HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Emoji).HasColumnName("emoji").IsRequired().HasMaxLength(50);

        builder.HasIndex(x => new { x.ResourceType, x.ResourceId }).HasDatabaseName("idx_reactions_resource");
        builder.HasIndex(x => new { x.ResourceId, x.UserId, x.Emoji }).IsUnique().HasDatabaseName("idx_reactions_unique");
    }
}
