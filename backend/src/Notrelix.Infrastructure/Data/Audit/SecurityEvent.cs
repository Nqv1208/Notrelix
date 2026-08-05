using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Audit;

public sealed class SecurityEvent
{
    public Guid Id { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? UserId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Severity { get; private set; } = "Info";
    public string Outcome { get; private set; } = "Observed";
    public int? RiskScore { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceId { get; private set; }
    public Guid? SessionId { get; private set; }
    public string? ResourceKind { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string? CorrelationId { get; private set; }
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public DateTimeOffset? RetentionUntil { get; private set; }

    private SecurityEvent() { }

    public SecurityEvent(
        Guid? workspaceId,
        Guid? userId,
        string eventType,
        string severity,
        string outcome,
        int? riskScore,
        string? ipAddress,
        string? userAgent,
        string? deviceId,
        Guid? sessionId,
        string? resourceType,
        Guid? resourceId,
        string? correlationId,
        JsonDocument? metadataJson,
        DateTimeOffset occurredAt)
    {
        Id = Guid.CreateVersion7();
        WorkspaceId = workspaceId;
        UserId = userId;
        EventType = eventType;
        Severity = severity;
        Outcome = outcome;
        RiskScore = riskScore;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        DeviceId = deviceId;
        SessionId = sessionId;
        ResourceKind = resourceType;
        ResourceId = resourceId;
        CorrelationId = correlationId;
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
        OccurredAt = occurredAt;
        RecordedAt = occurredAt;
    }
}
