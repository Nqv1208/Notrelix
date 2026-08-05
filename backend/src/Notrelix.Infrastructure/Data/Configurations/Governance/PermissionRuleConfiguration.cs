using Notrelix.Domain.Governance.Permissions;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class PermissionRuleConfiguration : IEntityTypeConfiguration<PermissionRule>
{
    public void Configure(EntityTypeBuilder<PermissionRule> builder)
    {
        builder.ToTable("permission_rules", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").IsRequired().HasMaxLength(50);
        builder.Property(x => x.ResourceKind).HasColumnName("resource_type").HasConversion<ResourceKindConverter>().HasMaxLength(128);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.SubjectType).HasColumnName("subject_type").IsRequired().HasMaxLength(50);
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
        builder.Property(x => x.SubjectKey).HasColumnName("subject_key").HasMaxLength(256);
        builder.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Effect).HasColumnName("effect").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.ConditionJson).HasColumnName("condition_json").HasColumnType("jsonb");
        builder.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(100);
        builder.Property(x => x.StartsAt).HasColumnName("starts_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(50);
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().HasDefaultValue(1L);

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_permission_rules_workspace_id");
        builder.HasIndex(x => new { x.ScopeType, x.Action }).HasDatabaseName("idx_permission_rules_scope_action");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_permission_rules_status");
    }
}
