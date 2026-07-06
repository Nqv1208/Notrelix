using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Data.Configurations.Ops;

public sealed class ExportJobRecordConfiguration : IEntityTypeConfiguration<ExportJobRecord>
{
    public void Configure(EntityTypeBuilder<ExportJobRecord> builder)
    {
        builder.ToTable("export_jobs", DbSchemas.Ops);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.JobType).HasColumnName("job_type").IsRequired().HasMaxLength(80);
        builder.Property(x => x.SourceResourceType).HasColumnName("source_resource_type").HasMaxLength(80);
        builder.Property(x => x.SourceResourceId).HasColumnName("source_resource_id");
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(40);
        builder.Property(x => x.Format).HasColumnName("format").IsRequired().HasMaxLength(20);
        builder.Property(x => x.RowCount).HasColumnName("row_count");
        builder.Property(x => x.OptionsJson).HasColumnName("options_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.FiltersJson).HasColumnName("filters_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ResultAttachmentId).HasColumnName("result_attachment_id");
        builder.Property(x => x.ResultFileId).HasColumnName("result_file_id");
        builder.Property(x => x.StorageProvider).HasColumnName("storage_provider").HasMaxLength(80);
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(500);
        builder.Property(x => x.DownloadUrl).HasColumnName("download_url").HasMaxLength(2000);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id");
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => x.WorkspaceId)
            .HasDatabaseName("ix_export_jobs_workspace_id");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_export_jobs_status");
    }
}