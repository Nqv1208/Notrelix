using Notrelix.Domain.Analytics.Snapshots;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Analytics;

public class ReportingSnapshotConfiguration : IEntityTypeConfiguration<ReportingSnapshot>
{
    public void Configure(EntityTypeBuilder<ReportingSnapshot> builder)
    {
        builder.ToTable("reporting_snapshots", DbSchemas.Reporting);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.ReportType).HasColumnName("report_type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("jsonb").HasConversion<JsonValueConverter>().IsRequired();
        builder.Property(x => x.CapturedAt).HasColumnName("captured_at").IsRequired();

        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_reporting_snapshots_workspace_id");
    }
}
