using Notrelix.Infrastructure.Data.Audit;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Infrastructure.Data.Abstractions;

public interface IAuditDbContext
{
    DbSet<AuditLog> EnterpriseAuditLogs { get; }
    DbSet<SecurityEvent> EnterpriseSecurityEvents { get; }
}