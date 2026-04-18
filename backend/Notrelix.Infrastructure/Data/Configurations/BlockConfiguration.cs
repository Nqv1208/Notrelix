using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities;

namespace Notrelix.Infrastructure.Data.Configurations;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id");

        builder.Property(b => b.PageId)
            .HasColumnName("page_id")
            .IsRequired();

        builder.Property(b => b.Type)
            .HasColumnName("type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Properties)
            .HasColumnName("properties")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(b => b.Position)
            .HasColumnName("position")
            .HasDefaultValue(0d);

        builder.Property(b => b.ParentBlockId)
            .HasColumnName("parent_block_id");

        builder.Property(b => b.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(b => b.Version)
            .HasColumnName("version")
            .HasDefaultValue(1);

        // Audit fields
        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(b => b.CreatedByUserId)
            .HasColumnName("created_by");

        builder.Property(b => b.UpdatedBy)
            .HasColumnName("updated_by");

        // Self-referencing relationship (nested blocks)
        builder.HasOne(b => b.ParentBlock)
            .WithMany(b => b.Children)
            .HasForeignKey(b => b.ParentBlockId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationships
        builder.HasOne(b => b.Page)
            .WithMany(p => p.Blocks)
            .HasForeignKey(b => b.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(b => new { b.PageId, b.Position })
            .HasDatabaseName("idx_blocks_page_position")
            .HasFilter("is_deleted = false");
        builder.HasIndex(b => b.ParentBlockId)
            .HasDatabaseName("idx_blocks_parent")
            .HasFilter("parent_block_id IS NOT NULL AND is_deleted = false");

        builder.Ignore(b => b.CreatedBy);
        builder.Ignore(b => b.UpdatedBy);

        // Ignore domain events
        builder.Ignore(b => b.DomainEvents);
    }
}
