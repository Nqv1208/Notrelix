using Notrelix.Domain.Common;
using Notrelix.Domain.Entities.Workspaces;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Extensibility;

public class AutomationExecution : AuditableEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid? AutomationRuleId { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public ResourceType? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public AutomationExecutionStatus Status { get; private set; } = AutomationExecutionStatus.Pending;
    public int AttemptCount { get; private set; }
    public string Payload { get; private set; } = "{}";
    public string? Response { get; private set; }
    public string? Error { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? FailedAt { get; private set; }

    public Workspace Workspace { get; private set; } = null!;
    public AutomationRule? AutomationRule { get; private set; }

    private AutomationExecution() { }

    public static AutomationExecution CreatePending(
        Guid workspaceId,
        Guid? automationRuleId,
        Guid eventId,
        string eventType,
        ResourceType? resourceType,
        Guid? resourceId,
        string payload)
    {
        return new AutomationExecution
        {
            WorkspaceId = workspaceId,
            AutomationRuleId = automationRuleId,
            EventId = eventId,
            EventType = string.IsNullOrWhiteSpace(eventType) ? throw new ArgumentException("Event type is required.", nameof(eventType)) : eventType.Trim().ToLowerInvariant(),
            ResourceType = resourceType,
            ResourceId = resourceId,
            Payload = string.IsNullOrWhiteSpace(payload) ? "{}" : payload,
            Status = AutomationExecutionStatus.Pending
        };
    }

    public void MarkDelivered(string? response)
    {
        Status = AutomationExecutionStatus.Delivered;
        Response = response;
        Error = null;
        DeliveredAt = DateTime.UtcNow;
    }

    public void MarkFailed(string? error)
    {
        Status = AutomationExecutionStatus.Failed;
        Error = error;
        FailedAt = DateTime.UtcNow;
    }

    public void MarkRetried(string? error)
    {
        Status = AutomationExecutionStatus.Retried;
        AttemptCount++;
        Error = error;
    }
}
