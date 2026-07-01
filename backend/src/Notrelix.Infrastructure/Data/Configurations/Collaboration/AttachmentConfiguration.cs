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

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired().HasMaxLength(50);

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.ResourceType).HasColumnName("resource_type").HasConversion<string>().IsRequired().HasMaxLength(50);
            target.Property(t => t.ResourceId).HasColumnName("resource_id").IsRequired();
            target.Property(t => t.WorkspaceId).HasColumnName("target_workspace_id");
            target.HasIndex(t => new { t.ResourceType, t.ResourceId }).HasDatabaseName("idx_attachments_resource");
        });

        builder.OwnsOne(x => x.Metadata, metadata =>
        {
            metadata.Property(m => m.FileName).HasColumnName("file_name").IsRequired().HasMaxLength(512);
            metadata.Property(m => m.Size).HasColumnName("file_size");
            metadata.Property(m => m.ContentType).HasColumnName("mime_type").HasMaxLength(128);
            metadata.Property(m => m.StorageKey).HasColumnName("storage_key").HasMaxLength(1024);
            metadata.Property(m => m.Url).HasColumnName("url").HasMaxLength(2048);
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
    }
}
