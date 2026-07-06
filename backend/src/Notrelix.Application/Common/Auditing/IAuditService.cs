namespace Notrelix.Application.Common.Auditing;

public interface IAuditService
{
    Task RecordAsync(
        Guid workspaceId,
        Guid actorId,
        string action,
        ResourceRef target,
        AuditMetadata metadata,
        AuditSeverity severity,
        string ipAddress = "",
        string userAgent = "",
        CancellationToken cancellationToken = default);
}
