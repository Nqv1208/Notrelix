namespace Notrelix.Application.Common.Auditing;

/// <summary>The person or group an audited change was made to, and the facts before and after it (never a secret).</summary>
public sealed record AuditChange(string? SubjectType, Guid? SubjectId, object? Before, object? After);

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
        CancellationToken cancellationToken = default,
        AuditChange? change = null);
}
