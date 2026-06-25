using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Governance.Audit;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Auditing;

internal sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditService(ApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task RecordAsync(
        Guid workspaceId,
        Guid actorId,
        string action,
        ResourceRef target,
        AuditMetadata metadata,
        AuditSeverity severity,
        string ipAddress = "",
        string userAgent = "",
        CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.Record(
            workspaceId,
            actorId,
            action,
            target,
            metadata,
            severity,
            ipAddress,
            userAgent,
            _dateTimeProvider.UtcNow);

        _context.AuditLogs.Add(auditLog);
        await Task.CompletedTask;
    }
}
