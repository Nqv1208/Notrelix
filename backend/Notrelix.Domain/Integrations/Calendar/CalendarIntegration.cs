using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Calendar;

public class CalendarEventLink : Entity
{
    public Guid IntegrationId { get; private set; }
    public Guid InternalEventId { get; private set; }
    public string ExternalEventId { get; private set; } = null!;
    public string? ETag { get; private set; }

    private CalendarEventLink() : base() { }

    public static CalendarEventLink Create(Guid integrationId, Guid internalEventId, string externalEventId, string? eTag = null)
    {
        Guard.NotEmpty(integrationId);
        Guard.NotEmpty(internalEventId);
        Guard.NotNullOrWhiteSpace(externalEventId);

        return new CalendarEventLink
        {
            IntegrationId = integrationId,
            InternalEventId = internalEventId,
            ExternalEventId = externalEventId,
            ETag = eTag
        };
    }
}

public class CalendarIntegration : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public Guid ConnectionId { get; private set; }
    public CalendarProvider Provider { get; private set; }
    public CalendarSyncDirection SyncDirection { get; private set; }
    public bool IsActive { get; private set; }

    private CalendarIntegration() : base() { }

    public static CalendarIntegration Create(Guid workspaceId, Guid connectionId, CalendarProvider provider, CalendarSyncDirection syncDirection, Guid createdBy)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(connectionId);

        var integration = new CalendarIntegration
        {
            WorkspaceId = workspaceId,
            ConnectionId = connectionId,
            Provider = provider,
            SyncDirection = syncDirection,
            IsActive = true
        };

        integration.SetAuditOnCreate(createdBy);
        integration.AddDomainEvent(new CalendarIntegrationConnectedEvent(workspaceId, connectionId));

        return integration;
    }
}
