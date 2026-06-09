using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Document;

namespace Notrelix.Infrastructure.Data.Configurations.Document;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by");
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(500).HasDefaultValue("Untitled");
        builder.Property(x => x.IconType).HasColumnName("icon_type").HasMaxLength(20);
        builder.Property(x => x.IconValue).HasColumnName("icon_value").HasMaxLength(100);
        builder.Property(x => x.CoverUrl).HasColumnName("cover_url").HasMaxLength(500);
        builder.Property(x => x.Position).HasColumnName("position");
        builder.Property(x => x.Depth).HasColumnName("depth");
        builder.Property(x => x.IsTemplate).HasColumnName("is_template").HasDefaultValue(false);
        builder.Property(x => x.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.Property(x => x.Deadline).HasColumnName("deadline");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.WorkspaceId, x.IsDeleted, x.Position }).HasDatabaseName("idx_pages_workspace");
        builder.HasIndex(x => x.ParentId).HasDatabaseName("idx_pages_parent");

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Ignore(x => x.CreatedBy);
        builder.Ignore(x => x.UpdatedBy);
        builder.Ignore(x => x.DomainEvents);
    }
}
