using Notrelix.Infrastructure.Data.Projections.Activity;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Infrastructure.Data.Abstractions;

public interface IActivityProjectionDbContext
{
    DbSet<WorkspaceActivityLogRecord> WorkspaceActivityLogs { get; }
    DbSet<ActivityReadStateRecord> ActivityReadStates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}