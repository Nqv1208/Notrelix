namespace Notrelix.Domain.Governance.Audit;

public class AuditLog : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; } = null!;
    public ResourceRef Target { get; private set; } = null!;
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
        ResourceRef target,
        AuditMetadata metadata,
        AuditSeverity severity,
        string ipAddress,
        string userAgent,
        DateTimeOffset timestamp)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(actorId);
        Guard.NotNullOrWhiteSpace(action);
        Guard.MaxLength(action, 255);
        Guard.NotNull(target);

        target.EnsureSameWorkspace(workspaceId);

        var log = new AuditLog
        {
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Action = action.Trim(),
            Target = target,
            Metadata = metadata,
            Severity = severity,
            Timestamp = timestamp,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        log.AddDomainEvent(new AuditLogRecordedDomainEvent(log.Id, workspaceId, log.Action, timestamp));

        return log;
    }

    // No Update or Delete methods provided to enforce append-only rule.
}
