using Notrelix.Infrastructure.Data.Ops.Entities;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Infrastructure.Data.Abstractions;

public interface IOpsDbContext
{
    DbSet<ImportJobRecord> ImportJobs { get; }
    DbSet<ExportJobRecord> ExportJobs { get; }
    DbSet<JobLockRecord> JobLocks { get; }
}
