using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.Infrastructure.Data.Configurations.Documents;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.PageId).HasColumnName("page_id").IsRequired();
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.OwnsOne(x => x.Content, content =>
        {
            content.Property(c => c.Value).HasColumnName("content").HasColumnType("jsonb").IsRequired();
        });

        builder.OwnsOne(x => x.Properties, props =>
        {
            props.Property(p => p.Value).HasColumnName("properties").HasColumnType("jsonb").IsRequired();
        });

        builder.OwnsOne(x => x.Position, pos =>
        {
            pos.Property(p => p.Value).HasColumnName("position").HasColumnType("float8").IsRequired();
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

        builder.HasOne<Page>()
            .WithMany()
            .HasForeignKey(x => x.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Block>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PageId, x.Position }).HasFilter("is_deleted = false").HasDatabaseName("idx_blocks_page_position");
        builder.HasIndex(x => x.ParentId).HasFilter("parent_id IS NOT NULL").HasDatabaseName("idx_blocks_parent_id");
    }
}
