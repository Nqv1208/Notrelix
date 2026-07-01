using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Infrastructure.Data.Converters;

namespace Notrelix.Infrastructure.Data.Configurations.Analytics;

public class DashboardWidgetConfiguration : IEntityTypeConfiguration<DashboardWidget>
{
    public void Configure(EntityTypeBuilder<DashboardWidget> builder)
    {
        builder.ToTable("dashboard_widgets", DbSchemas.Reporting);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
        builder.Property(x => x.WorkspaceId).HasColumnName("workspace_id").IsRequired();
        builder.Property(x => x.DashboardId).HasColumnName("dashboard_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").IsRequired().HasMaxLength(256);
        builder.Property(x => x.Type).HasColumnName("type").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb").HasConversion<JsonValueConverter>().IsRequired();

        builder.OwnsOne(x => x.Position, p =>
        {
            p.Property(w => w.X).HasColumnName("pos_x").IsRequired();
            p.Property(w => w.Y).HasColumnName("pos_y").IsRequired();
            p.Property(w => w.W).HasColumnName("pos_w").IsRequired();
            p.Property(w => w.H).HasColumnName("pos_h").IsRequired();
        });

        builder.HasIndex(x => x.DashboardId).HasDatabaseName("idx_dashboard_widgets_dashboard_id");
    }
}
