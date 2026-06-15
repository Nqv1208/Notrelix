using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class UsageMetricConfiguration : IEntityTypeConfiguration<UsageMetric>
{
    public void Configure(EntityTypeBuilder<UsageMetric> builder)
    {
        builder.ToTable("usage_metrics", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.Key).HasColumnName("metric_key").HasConversion<UsageMetricKeyConverter>().IsRequired().HasMaxLength(128);
        builder.Property(x => x.CurrentValue).HasColumnName("current_value").IsRequired();

        builder.OwnsOne(x => x.CurrentPeriod, p =>
        {
            p.Property(up => up.Start).HasColumnName("period_start").IsRequired();
            p.Property(up => up.End).HasColumnName("period_end").IsRequired();
        });

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

        builder.HasMany(x => x.History)
            .WithOne()
            .HasForeignKey(x => x.MetricId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_usage_metrics_workspace_id");
    }
}
