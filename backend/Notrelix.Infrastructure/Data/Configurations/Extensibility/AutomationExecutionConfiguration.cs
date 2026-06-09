using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Extensibility;

namespace Notrelix.Infrastructure.Data.Configurations.Extensibility;

public class AutomationExecutionConfiguration : IEntityTypeConfiguration<AutomationExecution>
{
    public void Configure(EntityTypeBuilder<AutomationExecution> builder)
    {
        builder.ToTable("automation_executions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.AutomationRuleId).HasColumnName("automation_rule_id");
        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(120).IsRequired();
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.Response).HasColumnName("response").HasColumnType("jsonb");
        builder.Property(x => x.Error).HasColumnName("error").HasMaxLength(4000);
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(x => x.FailedAt).HasColumnName("failed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AutomationRule)
            .WithMany()
            .HasForeignKey(x => x.AutomationRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.AutomationRuleId, x.EventId })
            .IsUnique()
            .HasFilter("automation_rule_id IS NOT NULL")
            .HasDatabaseName("idx_automation_executions_rule_event");

        builder.HasIndex(x => new { x.WorkspaceId, x.CreatedAt })
            .HasDatabaseName("idx_automation_executions_workspace_created");

        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("idx_automation_executions_status_created");

        builder.Ignore(x => x.DomainEvents);
    }
}
