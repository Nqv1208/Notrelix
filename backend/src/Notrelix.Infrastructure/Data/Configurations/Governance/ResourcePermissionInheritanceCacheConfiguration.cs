using Notrelix.Infrastructure.Data.Governance.Projections;

namespace Notrelix.Infrastructure.Data.Configurations.Governance;

public class ResourcePermissionInheritanceCacheConfiguration : IEntityTypeConfiguration<ResourcePermissionInheritanceCacheEntry>
{
    public void Configure(EntityTypeBuilder<ResourcePermissionInheritanceCacheEntry> builder)
    {
        builder.ToTable("resource_permission_inheritance_cache", DbSchemas.Governance);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").IsRequired().HasMaxLength(80);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.ParentResourceType).HasColumnName("parent_resource_type").HasMaxLength(80);
        builder.Property(x => x.ParentResourceId).HasColumnName("parent_resource_id");
        builder.Property(x => x.SubjectType).HasColumnName("subject_type").IsRequired().HasMaxLength(80);
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
        builder.Property(x => x.SubjectKey).HasColumnName("subject_key").HasMaxLength(160);
        builder.Property(x => x.Action).HasColumnName("action").IsRequired().HasMaxLength(160);
        builder.Property(x => x.Effect).HasColumnName("effect").IsRequired().HasMaxLength(20).HasDefaultValue("Allow");
        builder.Property(x => x.PermissionLevel).HasColumnName("permission_level").HasMaxLength(40);
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(80);
        builder.Property(x => x.SourceId).HasColumnName("source_id");
        builder.Property(x => x.InheritedFromResourceType).HasColumnName("inherited_from_resource_type").HasMaxLength(80);
        builder.Property(x => x.InheritedFromResourceId).HasColumnName("inherited_from_resource_id");
        builder.Property(x => x.CacheVersion).HasColumnName("cache_version").IsRequired().HasDefaultValue(1L);
        builder.Property(x => x.ComputedPermissionsJson).HasColumnName("computed_permissions_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.ComputedAt).HasColumnName("computed_at").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");

        builder.HasIndex(x => new
        {
            x.WorkspaceId,
            x.ResourceType,
            x.ResourceId,
            x.SubjectType,
            x.SubjectId,
            x.SubjectKey,
            x.Action,
        }).IsUnique().HasDatabaseName("ux_governance_permission_inheritance_cache");

        builder.HasIndex(x => new
        {
            x.WorkspaceId,
            x.SubjectType,
            x.SubjectId,
            x.ResourceType,
            x.ResourceId,
            x.Action,
        }).HasDatabaseName("ix_governance_permission_inheritance_cache_lookup");
    }
}
