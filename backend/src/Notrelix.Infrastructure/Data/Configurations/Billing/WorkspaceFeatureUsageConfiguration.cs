using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class WorkspaceFeatureUsageConfiguration : IEntityTypeConfiguration<WorkspaceFeatureUsage>
{
    public void Configure(EntityTypeBuilder<WorkspaceFeatureUsage> builder)
    {
        builder.ToTable("workspace_feature_usages", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Feature).HasColumnName("feature_code").HasConversion(v => v.Code, v => FeatureCode.Create(v)).IsRequired().HasMaxLength(128);
        builder.Property(x => x.CurrentUsage).HasColumnName("current_usage").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.HardLimit).HasColumnName("hard_limit").HasColumnType("decimal(18,2)");
        builder.Property(x => x.SoftLimit).HasColumnName("soft_limit").HasColumnType("decimal(18,2)");
        builder.Property(x => x.OverageAllowed).HasColumnName("overage_allowed");
        builder.Property(x => x.ResetPeriod).HasColumnName("reset_period").IsRequired().HasMaxLength(50);
        builder.Property(x => x.LastResetAt).HasColumnName("last_reset_at");

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

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_workspace_feature_usages_workspace_id");
    }
}
