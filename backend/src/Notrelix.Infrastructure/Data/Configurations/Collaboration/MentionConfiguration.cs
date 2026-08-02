using Notrelix.Domain.Collaboration.Mentions;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class MentionConfiguration : IEntityTypeConfiguration<Mention>
{
    public void Configure(EntityTypeBuilder<Mention> builder)
    {
        builder.ToTable("mentions", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.MentionedId).HasColumnName("mentioned_user_id").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.OwnsOne(x => x.Source, source =>
        {
            source.Property(s => s.Kind).HasColumnName("source_type").HasConversion(v => v.Value, v => LegacyResourceTypeMappings.ParseResourceKind(v)).IsRequired().HasMaxLength(128);
            source.Property(s => s.ResourceId).HasColumnName("source_id").IsRequired();
            source.Property(s => s.WorkspaceId).HasColumnName("source_workspace_id");
            source.HasIndex(s => new { s.Kind, s.ResourceId }).HasDatabaseName("idx_mentions_source");
        });

        builder.HasIndex(x => x.MentionedId).HasDatabaseName("idx_mentions_mentioned_user_id");
    }
}
