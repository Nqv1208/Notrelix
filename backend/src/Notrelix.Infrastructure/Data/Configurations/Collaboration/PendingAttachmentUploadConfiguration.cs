using Notrelix.Domain.Collaboration.Attachments;

using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Collaboration;

public class PendingAttachmentUploadConfiguration : IEntityTypeConfiguration<PendingAttachmentUpload>
{
    public void Configure(EntityTypeBuilder<PendingAttachmentUpload> builder)
    {
        builder.ToTable("attachment_pending_uploads", DbSchemas.Collab);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();

        builder.OwnsOne(x => x.Target, target =>
        {
            target.Property(t => t.Kind).HasColumnName("resource_type").HasConversion<ResourceKindConverter>().IsRequired().HasMaxLength(128);
            target.Property(t => t.ResourceId).HasColumnName("resource_id").IsRequired();
            target.Property(t => t.WorkspaceId).HasColumnName("target_workspace_id");
            target.HasIndex(t => new { t.Kind, t.ResourceId }).HasDatabaseName("idx_attachment_pending_uploads_resource");
        });

        builder.Property(x => x.StorageKey).HasColumnName("storage_key").IsRequired().HasMaxLength(1024);
        builder.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("ux_attachment_pending_uploads_storage_key");

        builder.Property(x => x.FileName).HasColumnName("file_name").IsRequired().HasMaxLength(512);
        builder.Property(x => x.DeclaredSize).HasColumnName("declared_size").IsRequired();
        builder.Property(x => x.CeilingBytes).HasColumnName("ceiling_bytes").IsRequired();
        builder.Property(x => x.DeclaredContentType).HasColumnName("declared_content_type").IsRequired().HasMaxLength(128);
        builder.Property(x => x.Voice).HasColumnName("voice").IsRequired();
        builder.Property(x => x.DurationHintMs).HasColumnName("duration_hint_ms");
        builder.Property(x => x.Waveform).HasColumnName("waveform").HasMaxLength(64);

        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.AttachmentId).HasColumnName("attachment_id");
        builder.Property(x => x.CommittedAt).HasColumnName("committed_at");

        // The expiry sweep reads the uncommitted rows only, oldest expiry first.
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_attachment_pending_uploads_expiry")
            .HasFilter("attachment_id IS NULL");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
    }
}
