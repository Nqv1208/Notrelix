using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Mentions;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class MentionConfiguration : IEntityTypeConfiguration<Mention>
{
    public void Configure(EntityTypeBuilder<Mention> builder)
    {
        builder.ToTable("mentions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.SourceType).HasColumnName("source_type").IsRequired().HasMaxLength(50);
        builder.Property(x => x.SourceId).HasColumnName("source_id").IsRequired();
        builder.Property(x => x.MentionedUserId).HasColumnName("mentioned_user_id").IsRequired();
        builder.Property(x => x.MentionedBy).HasColumnName("mentioned_by").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => x.MentionedUserId).HasDatabaseName("idx_mentions_mentioned_user_id");
        builder.HasIndex(x => new { x.SourceType, x.SourceId }).HasDatabaseName("idx_mentions_source");
    }
}
