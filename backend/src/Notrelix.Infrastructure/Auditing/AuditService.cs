using Notrelix.Domain.Common.Auditing;
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
        var now = _dateTimeProvider.UtcNow;
        var auditLog = new Data.Audit.AuditLog(
            workspaceId: workspaceId,
            actorUserId: actorId,
            actorType: "User",
            action: action,
            resourceType: target.ResourceType.ToString(),
            resourceId: target.ResourceId,
            subjectType: null,
            subjectId: null,
            severity: severity.ToString(),
            outcome: "Succeeded",
            ipAddress: string.IsNullOrEmpty(ipAddress) ? metadata.IpAddress : ipAddress,
            userAgent: string.IsNullOrEmpty(userAgent) ? metadata.UserAgent : userAgent,
            requestId: metadata.TraceId,
            correlationId: null,
            causationId: null,
            beforeJson: null,
            afterJson: null,
            metadataJson: null,
            occurredAt: now);

        _context.EnterpriseAuditLogs.Add(auditLog);
        await Task.CompletedTask;
    }
}
