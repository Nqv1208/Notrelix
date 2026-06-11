using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Audit;

public class AuditLog : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; } = null!;
    public string ResourceType { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public AuditMetadata Metadata { get; private set; } = null!;
    public AuditSeverity Severity { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string IpAddress { get; private set; } = null!;
    public string UserAgent { get; private set; } = null!;

    private AuditLog() : base() { }

    public static AuditLog Record(
        Guid workspaceId,
        Guid actorId,
        string action,
        string resourceType,
        Guid resourceId,
        AuditMetadata metadata,
        AuditSeverity severity,
        string ipAddress,
        string userAgent,
        DateTimeOffset timestamp)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(actorId);
        Guard.NotNullOrWhiteSpace(action);
        Guard.NotNullOrWhiteSpace(resourceType);

        var log = new AuditLog
        {
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Action = action.Trim(),
            ResourceType = resourceType.Trim(),
            ResourceId = resourceId,
            Metadata = metadata,
            Severity = severity,
            Timestamp = timestamp,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        log.AddDomainEvent(new AuditLogRecordedEvent(log.Id, workspaceId, log.Action, timestamp));
        
        return log;
    }

    // No Update or Delete methods provided to enforce append-only rule.
}
