using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Infrastructure.Data.Configurations.Billing;

public class UsageMetricHistoryConfiguration : IEntityTypeConfiguration<UsageMetricHistory>
{
    public void Configure(EntityTypeBuilder<UsageMetricHistory> builder)
    {
        builder.ToTable("usage_metric_history", DbSchemas.Billing);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.MetricId).HasColumnName("metric_id").IsRequired();
        builder.Property(x => x.Increment).HasColumnName("increment").IsRequired();
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();

        builder.HasIndex(x => x.MetricId).HasDatabaseName("idx_usage_metric_history_metric_id");
    }
}
