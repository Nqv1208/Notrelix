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

    public void UpdateETag(string? eTag)
    {
        ETag = eTag;
    }
}

public class CalendarIntegration : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid ConnectionId { get; private set; }
    public CalendarProvider Provider { get; private set; }
    public CalendarSyncDirection SyncDirection { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<CalendarEventLink> _eventLinks = new();
    public IReadOnlyCollection<CalendarEventLink> EventLinks => _eventLinks.AsReadOnly();

    private CalendarIntegration() : base() { }

    public static CalendarIntegration Create(Guid accountId, Guid workspaceId, Guid connectionId, CalendarProvider provider, CalendarSyncDirection syncDirection, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(connectionId);

        var integration = new CalendarIntegration
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ConnectionId = connectionId,
            Provider = provider,
            SyncDirection = syncDirection,
            IsActive = true
        };

        integration.SetAuditOnCreate(createdBy, createdAt);
        integration.AddDomainEvent(new CalendarIntegrationConnectedDomainEvent(accountId, workspaceId, connectionId, createdAt));

        return integration;
    }

    public void Activate(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (IsActive) return;

        IsActive = true;
        SetAuditOnUpdate(updatedBy, occurredAt);
        AddDomainEvent(new CalendarIntegrationActivatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void Deactivate(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (!IsActive) return;

        IsActive = false;
        SetAuditOnUpdate(updatedBy, occurredAt);
        AddDomainEvent(new CalendarIntegrationDeactivatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void ChangeSyncDirection(CalendarSyncDirection newDirection, Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (!IsActive)
            throw new DomainException("Cannot change sync direction on a deactivated calendar integration.");
        if (SyncDirection == newDirection) return;

        SyncDirection = newDirection;
        SetAuditOnUpdate(updatedBy, occurredAt);
        AddDomainEvent(new CalendarIntegrationSyncDirectionChangedDomainEvent(AccountId, WorkspaceId, Id, newDirection, updatedBy, occurredAt));
    }

    public void LinkEvent(Guid internalEventId, string externalEventId, string? eTag = null)
    {
        EnsureNotDeleted();
        if (!IsActive)
            throw new DomainException("Cannot link events on a deactivated calendar integration.");
        Guard.NotEmpty(internalEventId);
        Guard.NotNullOrWhiteSpace(externalEventId);

        if (_eventLinks.Any(l => l.InternalEventId == internalEventId || l.ExternalEventId == externalEventId))
        {
            throw new DomainException("An event link already exists for this internal or external event.");
        }

        _eventLinks.Add(CalendarEventLink.Create(Id, internalEventId, externalEventId, eTag));
    }

    public void UpdateEventLinkETag(Guid internalEventId, string? newETag)
    {
        EnsureNotDeleted();
        var link = _eventLinks.FirstOrDefault(l => l.InternalEventId == internalEventId);
        if (link == null)
        {
            throw new DomainException($"No event link found for internal event '{internalEventId}'.");
        }
        link.UpdateETag(newETag);
    }
}
