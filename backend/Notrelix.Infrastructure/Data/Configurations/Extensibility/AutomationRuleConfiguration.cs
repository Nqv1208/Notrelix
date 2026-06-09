using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Entities.Extensibility;

namespace Notrelix.Infrastructure.Data.Configurations.Extensibility;

public class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.ToTable("automation_rules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(x => x.IntegrationConnectionId).HasColumnName("integration_connection_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.TriggerEvent).HasColumnName("trigger_event").HasMaxLength(120).IsRequired();
        builder.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Configuration).HasColumnName("configuration").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.IntegrationConnection)
            .WithMany()
            .HasForeignKey(x => x.IntegrationConnectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.WorkspaceId, x.TriggerEvent, x.IsEnabled })
            .HasDatabaseName("idx_automation_rules_workspace_trigger_enabled");

        builder.Ignore(x => x.DomainEvents);
    }
}
