using Notrelix.Domain.Automation.Scheduled;

namespace Notrelix.Infrastructure.Data.Configurations.Automation;

public class ScheduledJobConfiguration : IEntityTypeConfiguration<ScheduledJob>
{
    public void Configure(EntityTypeBuilder<ScheduledJob> builder)
    {
        builder.ToTable("scheduled_jobs", DbSchemas.Automation);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.RuleId).HasColumnName("rule_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.NextRunAt).HasColumnName("next_run_at");
        builder.Property(x => x.LastRunAt).HasColumnName("last_run_at");

        builder.OwnsOne(x => x.Schedule, s =>
        {
            s.Property(p => p.CronExpression).HasColumnName("cron_expression").IsRequired().HasMaxLength(100);
            s.Property(p => p.TimeZone).HasColumnName("timezone").IsRequired().HasMaxLength(64).HasDefaultValue("UTC");
        });

        builder.Ignore(x => x.IsDeleted);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Property(x => x.DeleteReason).HasColumnName("delete_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_scheduled_jobs_workspace_id");
        builder.HasIndex(x => x.RuleId).HasDatabaseName("idx_scheduled_jobs_rule_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_scheduled_jobs_status");
    }
}
