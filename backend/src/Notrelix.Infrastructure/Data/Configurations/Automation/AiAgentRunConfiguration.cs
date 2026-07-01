using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Automation.Agents;

namespace Notrelix.Infrastructure.Data.Configurations.Automation;

public class AiAgentRunConfiguration : IEntityTypeConfiguration<AiAgentRun>
{
    public void Configure(EntityTypeBuilder<AiAgentRun> builder)
    {
        builder.ToTable("ai_agent_runs", DbSchemas.Automation);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.AiAgentId).HasColumnName("ai_agent_id").IsRequired();
        builder.Property(x => x.TriggerType).HasColumnName("trigger_type").IsRequired().HasMaxLength(50);
        builder.Property(x => x.TriggerResourceType).HasColumnName("trigger_resource_type").HasMaxLength(50);
        builder.Property(x => x.TriggerResourceId).HasColumnName("trigger_resource_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.Input).HasColumnName("input").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Output).HasColumnName("output").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Error).HasColumnName("error").HasColumnType("jsonb");
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.FinishedAt).HasColumnName("finished_at");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id");

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

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_ai_agent_runs_workspace_id");
        builder.HasIndex(x => x.AiAgentId).HasDatabaseName("idx_ai_agent_runs_agent_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("idx_ai_agent_runs_status");
    }
}
