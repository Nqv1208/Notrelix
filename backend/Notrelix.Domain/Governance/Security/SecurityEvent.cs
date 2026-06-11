using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Security;

public class SecurityEvent : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public SecurityEventType Type { get; private set; }
    public SecuritySeverity Severity { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public SecurityEventMetadata Metadata { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }

    private SecurityEvent() : base() { }

    public static SecurityEvent Record(
        Guid workspaceId, 
        SecurityEventType type, 
        SecuritySeverity severity, 
        string title, 
        SecurityEventMetadata metadata,
        DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(title);

        var @event = new SecurityEvent
        {
            WorkspaceId = workspaceId,
            Type = type,
            Severity = severity,
            Title = title,
            Metadata = metadata,
            OccurredAt = occurredAt
        };

        @event.AddDomainEvent(new SecurityEventRecordedEvent(@event.Id, workspaceId, type, occurredAt));

        return @event;
    }
}
