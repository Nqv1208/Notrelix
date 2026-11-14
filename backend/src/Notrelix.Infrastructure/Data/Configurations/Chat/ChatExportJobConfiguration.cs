using Notrelix.Domain.Chat.Exports;

namespace Notrelix.Infrastructure.Data.Configurations.Chat;

/// <summary>
/// The export jobs of a person's chat data (UC-CHT-H06): who asked, whose data, the range, the state and the storage key of the file. Counts
/// only; the file holds the data and lives in object storage. The status is stored as its wire text.
/// </summary>
public class ChatExportJobConfiguration : IEntityTypeConfiguration<ChatExportJob>
{
    public void Configure(EntityTypeBuilder<ChatExportJob> builder)
    {
        builder.ToTable("export_jobs", DbSchemas.Chat);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.RequestedBy).HasColumnName("requested_by").IsRequired();
        builder.Property(x => x.SubjectUserId).HasColumnName("subject_user_id").IsRequired();
        builder.Property(x => x.RangeFrom).HasColumnName("range_from");
        builder.Property(x => x.RangeTo).HasColumnName("range_to");
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(v => v.ToWire(), s => ChatExportStatusWire.FromWire(s))
            .IsRequired()
            .HasMaxLength(16);
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(512);
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired().HasDefaultValue(0L);
        builder.Property(x => x.MessageCount).HasColumnName("message_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.SpaceCount).HasColumnName("space_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ExcludedSpaceCount).HasColumnName("excluded_space_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TombstoneCount).HasColumnName("tombstone_count").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.FailureCode).HasColumnName("failure_code").HasMaxLength(64);
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // The worker takes the oldest queued job; the sweep finds ready files past their expiry.
        builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("ix_export_jobs_status_created");
        builder.HasIndex(x => new { x.WorkspaceId, x.RequestedBy, x.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_export_jobs_workspace_requester");
    }
}
