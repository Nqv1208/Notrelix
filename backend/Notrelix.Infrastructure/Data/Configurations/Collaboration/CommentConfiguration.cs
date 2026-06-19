using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CommentStatus).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.ResourceType).HasColumnName("resource_type").HasConversion<string>().IsRequired().HasMaxLength(50);
            target.Property(t => t.ResourceId).HasColumnName("resource_id").IsRequired();
            target.Property(t => t.WorkspaceId).HasColumnName("target_workspace_id");
            target.HasIndex(t => new { t.ResourceType, t.ResourceId }).HasDatabaseName("idx_comments_resource");
        });

        builder.OwnsOne(x => x.Anchor, anchor =>
        {
            anchor.Property(a => a.Selector).HasColumnName("anchor_selector").HasMaxLength(256);
            anchor.Property(a => a.Offset).HasColumnName("anchor_offset");
        });

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.RestoredAt).HasColumnName("restored_at");
        builder.Property(x => x.RestoredBy).HasColumnName("restored_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
