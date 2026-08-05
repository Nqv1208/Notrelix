using Notrelix.Domain.Documents.ResourceLinks;

using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Documents;

public class ResourceLinkConfiguration : IEntityTypeConfiguration<ResourceLink>
{
    public void Configure(EntityTypeBuilder<ResourceLink> builder)
    {
        builder.ToTable("resource_links", DbSchemas.Docs);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("link_type").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.OwnsOne(x => x.Source, s =>
        {
            s.Property(p => p.Kind).HasColumnName("source_type").HasConversion<ResourceKindConverter>().IsRequired().HasMaxLength(128);
            s.Property(p => p.ResourceId).HasColumnName("source_id").IsRequired();
            s.HasIndex(p => new { p.Kind, p.ResourceId }).HasDatabaseName("idx_resource_links_source");
        });

        builder.OwnsOne(x => x.Target, t =>
        {
            t.Property(p => p.Kind).HasColumnName("target_type").HasConversion<ResourceKindConverter>().IsRequired().HasMaxLength(128);
            t.Property(p => p.ResourceId).HasColumnName("target_id").IsRequired();
            t.HasIndex(p => new { p.Kind, p.ResourceId }).HasDatabaseName("idx_resource_links_target");
        });

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_resource_links_workspace_id");
    }
}
