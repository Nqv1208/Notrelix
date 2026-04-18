using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities;

namespace Notrelix.Infrastructure.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.ResourceType)
            .HasColumnName("resource_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.WorkspaceId)
            .HasColumnName("workspace_id")
            .IsRequired();

        builder.Property(c => c.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(c => c.Content)
            .HasColumnName("content_md")
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(c => c.ParentCommentId)
            .HasColumnName("parent_comment_id");

        builder.Property(c => c.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(c => c.ResolvedBy)
            .HasColumnName("resolved_by");

        builder.Property(c => c.IsEdited)
            .HasColumnName("is_edited")
            .HasDefaultValue(false);

        builder.Property(c => c.EditedAt)
            .HasColumnName("edited_at");

        builder.Property(c => c.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at");

        // Audit fields
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(c => c.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(c => c.UpdatedBy)
            .HasColumnName("updated_by");

        // Self-referencing relationship (replies)
        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => new { c.ResourceType, c.ResourceId, c.CreatedAt })
            .HasDatabaseName("idx_comments_resource");
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.ParentCommentId);

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Query filter for soft delete
        builder.HasQueryFilter(c => !c.IsDeleted);

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}
