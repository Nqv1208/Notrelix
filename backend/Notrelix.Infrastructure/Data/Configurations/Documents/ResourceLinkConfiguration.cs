using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Documents.ResourceLinks;

namespace Notrelix.Infrastructure.Data.Configurations.Documents;

public class ResourceLinkConfiguration : IEntityTypeConfiguration<ResourceLink>
{
    public void Configure(EntityTypeBuilder<ResourceLink> builder)
    {
        builder.ToTable("resource_links", DbSchemas.Docs);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("link_type").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.OwnsOne(x => x.Source, s =>
        {
            s.Property(p => p.ResourceType).HasColumnName("source_type").IsRequired().HasMaxLength(50);
            s.Property(p => p.ResourceId).HasColumnName("source_id").IsRequired();
        });

        builder.OwnsOne(x => x.Target, t =>
        {
            t.Property(p => p.ResourceType).HasColumnName("target_type").IsRequired().HasMaxLength(50);
            t.Property(p => p.ResourceId).HasColumnName("target_id").IsRequired();
        });

        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_resource_links_workspace_id");
        builder.HasIndex(x => new { x.Source.ResourceType, x.Source.ResourceId }).HasDatabaseName("idx_resource_links_source");
        builder.HasIndex(x => new { x.Target.ResourceType, x.Target.ResourceId }).HasDatabaseName("idx_resource_links_target");
    }
}
