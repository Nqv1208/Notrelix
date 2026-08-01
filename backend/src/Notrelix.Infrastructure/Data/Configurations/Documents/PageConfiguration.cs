using Notrelix.Domain.Documents.Pages;

namespace Notrelix.Infrastructure.Data.Configurations.Documents;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages", DbSchemas.Docs);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(1024);
        builder.Property(x => x.Icon).HasColumnName("icon").IsRequired().HasMaxLength(50).HasDefaultValue("\ud83d\udcc4");
        builder.Property(x => x.CoverImage).HasColumnName("cover_image");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Visibility).HasColumnName("visibility").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.WorkspaceId).HasFilter("deleted_at IS NULL").HasDatabaseName("idx_pages_workspace_id");
        builder.HasIndex(x => x.ParentId).HasFilter("parent_id IS NOT NULL AND deleted_at IS NULL").HasDatabaseName("idx_pages_parent_id");
    }
}
