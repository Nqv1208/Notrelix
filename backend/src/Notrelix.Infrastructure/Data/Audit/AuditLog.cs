using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Audit;

public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string ActorType { get; private set; } = "User";
    public string Action { get; private set; } = null!;
    public string? ResourceKind { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string? SubjectType { get; private set; }
    public Guid? SubjectId { get; private set; }
    public string Severity { get; private set; } = "Info";
    public string Outcome { get; private set; } = "Succeeded";
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? RequestId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }
    public JsonDocument? BeforeJson { get; private set; }
    public JsonDocument? AfterJson { get; private set; }
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public DateTimeOffset? RetentionUntil { get; private set; }

    private AuditLog() { }

    public AuditLog(
        Guid? workspaceId,
        Guid? actorUserId,
        string actorType,
        string action,
        string? resourceType,
        Guid? resourceId,
        string? subjectType,
        Guid? subjectId,
        string severity,
        string outcome,
        string? ipAddress,
        string? userAgent,
        string? requestId,
        string? correlationId,
        string? causationId,
        JsonDocument? beforeJson,
        JsonDocument? afterJson,
        JsonDocument? metadataJson,
        DateTimeOffset occurredAt)
    {
        Id = Guid.CreateVersion7();
        WorkspaceId = workspaceId;
        ActorUserId = actorUserId;
        ActorType = actorType;
        Action = action;
        ResourceKind = resourceType;
        ResourceId = resourceId;
        SubjectType = subjectType;
        SubjectId = subjectId;
        Severity = severity;
        Outcome = outcome;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        RequestId = requestId;
        CorrelationId = correlationId;
        CausationId = causationId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
        OccurredAt = occurredAt;
        RecordedAt = occurredAt;
    }
}
