using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Collaboration.Attachments;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").IsRequired().HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id").IsRequired();
        builder.Property(x => x.FileName).HasColumnName("file_name").IsRequired().HasMaxLength(512);
        builder.Property(x => x.FileSize).HasColumnName("file_size");
        builder.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(128);
        builder.Property(x => x.Url).HasColumnName("url").IsRequired().HasMaxLength(2048);
        builder.Property(x => x.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(2048);
        builder.Property(x => x.UploadedBy).HasColumnName("uploaded_by").IsRequired();

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

        builder.HasIndex(x => new { x.ResourceType, x.ResourceId }).HasDatabaseName("idx_attachments_resource");
        builder.HasIndex(x => x.UploadedBy).HasDatabaseName("idx_attachments_uploaded_by");
    }
}
