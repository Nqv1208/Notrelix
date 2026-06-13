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
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").IsRequired().HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.AuthorId).HasColumnName("author_id").IsRequired();
        builder.Property(x => x.ParentId).HasColumnName("parent_id");
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IsEdited).HasColumnName("is_edited");

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

        builder.HasOne<Comment>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ResourceType, x.ResourceId }).HasDatabaseName("idx_comments_resource");
        builder.HasIndex(x => x.AuthorId).HasDatabaseName("idx_comments_author_id");
    }
}
