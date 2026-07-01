using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;

namespace Notrelix.Infrastructure.Data.Configurations.Automation;

public class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecution>
{
    public void Configure(EntityTypeBuilder<AutomationExecution> builder)
    {
        builder.ToTable("automation_executions", DbSchemas.Automation);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.RuleId).HasColumnName("rule_id").IsRequired();
        builder.Property(x => x.TriggerId).HasColumnName("trigger_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(x => x.FinishedAt).HasColumnName("finished_at");
        builder.Property(x => x.Error).HasColumnName("error");
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count");
        builder.Property(x => x.LastResponse).HasColumnName("last_response");

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

        builder.HasMany(x => x.Steps)
            .WithOne()
            .HasForeignKey(x => x.ExecutionId);

        builder.HasOne<AutomationRule>()
            .WithMany()
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.RuleId).HasDatabaseName("idx_automation_executions_rule_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_automation_executions_status");
    }
}
