using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Analytics;

public class DashboardSourceConfiguration : IEntityTypeConfiguration<DashboardSource>
{
    public void Configure(EntityTypeBuilder<DashboardSource> builder)
    {
        builder.ToTable("dashboard_sources", DbSchemas.Reporting);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.DashboardId).HasColumnName("dashboard_id").IsRequired();
        builder.Property(x => x.SourceType).HasColumnName("source_type").HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(x => x.BoardId).HasColumnName("board_id");
        builder.Property(x => x.BoardViewId).HasColumnName("board_view_id");
        builder.Property(x => x.Filter).HasColumnName("filter").HasColumnType("jsonb").HasConversion<JsonValueConverter>().IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasIndex(x => x.DashboardId).HasDatabaseName("idx_dashboard_sources_dashboard_id");
        builder.HasIndex(x => x.WorkspaceId).HasDatabaseName("idx_dashboard_sources_workspace_id");
    }
}
