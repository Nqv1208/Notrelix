using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Infrastructure.Data.Ops.Entities;

namespace Notrelix.Infrastructure.Data.Configurations.Ops;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJobRecord>
{
    public void Configure(EntityTypeBuilder<ImportJobRecord> builder)
    {
        builder.ToTable("import_jobs", DbSchemas.Ops);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.JobType).HasColumnName("job_type").IsRequired().HasMaxLength(80);
        builder.Property(x => x.TargetResourceType).HasColumnName("target_resource_type").HasMaxLength(80);
        builder.Property(x => x.TargetResourceId).HasColumnName("target_resource_id");
        builder.Property(x => x.SourceFileAttachmentId).HasColumnName("source_file_attachment_id");
        builder.Property(x => x.Status).HasColumnName("status").IsRequired().HasMaxLength(40).HasDefaultValue("Pending");
        builder.Property(x => x.TotalRecords).HasColumnName("total_records").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ProcessedRecords).HasColumnName("processed_records").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.SucceededRecords).HasColumnName("succeeded_records").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.FailedRecords).HasColumnName("failed_records").IsRequired().HasDefaultValue(0);
        builder.Property(x => x.OptionsJson).HasColumnName("options_json").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.ResultJson).HasColumnName("result_json").HasColumnType("jsonb");
        builder.Property(x => x.ErrorSummary).HasColumnName("error_summary");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message");
        builder.Property(x => x.ErrorFileAttachmentId).HasColumnName("error_file_attachment_id");
        builder.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id");
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(x => new { x.WorkspaceId, x.Status, x.CreatedAt }).IsDescending(false, false, true).HasDatabaseName("ix_ops_import_jobs_workspace_status");
    }
}
