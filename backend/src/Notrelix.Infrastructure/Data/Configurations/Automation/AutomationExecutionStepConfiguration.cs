using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Automation.Executions;

namespace Notrelix.Infrastructure.Data.Configurations.Automation;

public class AutomationExecutionStepConfiguration : IEntityTypeConfiguration<AutomationExecutionStep>
{
    public void Configure(EntityTypeBuilder<AutomationExecutionStep> builder)
    {
        builder.ToTable("automation_execution_steps", DbSchemas.Automation);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.ExecutionId).HasColumnName("execution_id").IsRequired();
        builder.Property(x => x.ActionId).HasColumnName("action_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.FinishedAt).HasColumnName("finished_at");
        builder.Property(x => x.Error).HasColumnName("error");

        builder.HasOne<AutomationExecution>()
            .WithMany(x => x.Steps)
            .HasForeignKey(x => x.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ExecutionId).HasDatabaseName("idx_automation_execution_steps_execution_id");
    }
}
