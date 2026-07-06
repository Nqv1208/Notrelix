using Notrelix.Domain.Analytics.Dashboards;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.Analytics.Abstractions;

public interface IReportingDbContext
{
    DbSet<Dashboard> Dashboards { get; }
    DbSet<DashboardWidget> DashboardWidgets { get; }
    DbSet<DashboardSource> DashboardSources { get; }
    DbSet<ReportingSnapshot> ReportingSnapshots { get; }
}