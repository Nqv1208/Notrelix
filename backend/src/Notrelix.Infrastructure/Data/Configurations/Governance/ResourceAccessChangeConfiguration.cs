using Notrelix.Domain.Governance.AccessChanges;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class ResourceAccessChangeConfiguration : IEntityTypeConfiguration<ResourceAccessChange>
{
    public void Configure(EntityTypeBuilder<ResourceAccessChange> builder)
    {
        builder.ToTable("resource_access_changes", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ResourceKind).HasColumnName("resource_type").HasConversion<ResourceKindConverter>().IsRequired().HasMaxLength(128);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.Kind).HasColumnName("change_kind").HasConversion<string>().IsRequired().HasMaxLength(40);
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id").IsRequired();
        builder.Property(x => x.SubjectType).HasColumnName("subject_type").HasMaxLength(20);
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
        builder.Property(x => x.FromLevel).HasColumnName("from_level").HasMaxLength(20);
        builder.Property(x => x.ToLevel).HasColumnName("to_level").HasMaxLength(20);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(x => x.RetentionUntil).HasColumnName("retention_until").IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.ResourceKind, x.ResourceId, x.OccurredAt })
            .IsDescending(false, false, false, true)
            .HasDatabaseName("idx_resource_access_changes_resource");
        builder.HasIndex(x => x.RetentionUntil).HasDatabaseName("idx_resource_access_changes_retention");
    }
}
