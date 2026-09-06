using Notrelix.Domain.Governance.Permissions;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class ResourcePermissionConfiguration : IEntityTypeConfiguration<ResourcePermission>
{
    public void Configure(EntityTypeBuilder<ResourcePermission> builder)
    {
        builder.ToTable("resource_permissions", DbSchemas.Governance);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ResourceKind).HasColumnName("resource_type").HasConversion<ResourceKindConverter>().IsRequired().HasMaxLength(128);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.SubjectType).HasColumnName("subject_type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.SubjectId).HasColumnName("subject_id").IsRequired();
        builder.Property(x => x.Level).HasColumnName("permission_level").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Effect).HasColumnName("effect").HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(x => x.ConditionJson).HasColumnName("condition_json").HasColumnType("jsonb");
        builder.Property(x => x.Priority).HasColumnName("priority").HasDefaultValue(100);

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => new { x.ResourceKind, x.ResourceId }).HasDatabaseName("idx_resource_permissions_resource");
        builder.HasIndex(x => x.SubjectId).HasDatabaseName("idx_resource_permissions_subject_id");
        builder.HasIndex(x => new
        {
            x.WorkspaceId,
            x.ResourceKind,
            x.ResourceId,
            x.SubjectType,
            x.SubjectId
        })
            .HasFilter("deleted_at IS NULL")
            .IsUnique()
            .HasDatabaseName("uq_resource_permissions_active_subject");
    }
}
