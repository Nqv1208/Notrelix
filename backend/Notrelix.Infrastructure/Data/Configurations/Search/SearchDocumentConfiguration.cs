using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Data.Configurations.Search;

public class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocumentRecord>
{
    public void Configure(EntityTypeBuilder<SearchDocumentRecord> builder)
    {
        builder.ToTable("search_documents", DbSchemas.Search);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").IsRequired().HasMaxLength(80);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired();
        builder.Property(x => x.Content).HasColumnName("content");
        builder.Property(x => x.Tags).HasColumnName("tags").HasColumnType("text[]");
        builder.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.SearchVector).HasColumnName("search_vector");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.WorkspaceId, x.ResourceType }).HasDatabaseName("ix_search_documents_workspace_type");
        builder.HasIndex(x => new { x.WorkspaceId, x.ResourceType, x.ResourceId }).IsUnique().HasDatabaseName("ux_search_documents_resource");
    }
}
