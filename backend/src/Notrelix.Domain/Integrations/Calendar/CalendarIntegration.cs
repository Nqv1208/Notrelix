using Notrelix.Domain.Integrations.Calendar.Events;
using static Notrelix.Domain.Integrations.IntegrationRuleCodes;

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

public class CalendarIntegration : SoftDeletableAggregateRoot, IWorkspaceScoped
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
        integration.RaiseDomainEvent(new CalendarIntegrationConnectedDomainEvent(accountId, workspaceId, connectionId, createdAt));

        return integration;
    }

    public void Activate(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (IsActive) return;

        var pending = PrepareAuditUpdate(updatedBy, occurredAt);
        IsActive = true;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CalendarIntegrationActivatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void Deactivate(Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (!IsActive) return;

        var pending = PrepareAuditUpdate(updatedBy, occurredAt);
        IsActive = false;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CalendarIntegrationDeactivatedDomainEvent(AccountId, WorkspaceId, Id, updatedBy, occurredAt));
    }

    public void ChangeSyncDirection(CalendarSyncDirection newDirection, Guid updatedBy, DateTimeOffset occurredAt)
    {
        EnsureNotDeleted();
        if (!IsActive)
            throw new BusinessRuleException(Integrations_Calendar_CannotChangeDirectionDeactivated, "Cannot change sync direction on a deactivated calendar integration.");
        if (SyncDirection == newDirection) return;

        var pending = PrepareAuditUpdate(updatedBy, occurredAt);
        SyncDirection = newDirection;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new CalendarIntegrationSyncDirectionChangedDomainEvent(AccountId, WorkspaceId, Id, newDirection, updatedBy, occurredAt));
    }

    public void LinkEvent(Guid internalEventId, string externalEventId, string? eTag = null)
    {
        EnsureNotDeleted();
        if (!IsActive)
            throw new BusinessRuleException(Integrations_Calendar_CannotLinkEventsDeactivated, "Cannot link events on a deactivated calendar integration.");
        Guard.NotEmpty(internalEventId);
        Guard.NotNullOrWhiteSpace(externalEventId);

        if (_eventLinks.Any(l => l.InternalEventId == internalEventId || l.ExternalEventId == externalEventId))
        {
            throw new BusinessRuleException(Integrations_Calendar_EventLinkAlreadyExists, "An event link already exists for this internal or external event.");
        }

        _eventLinks.Add(CalendarEventLink.Create(Id, internalEventId, externalEventId, eTag));
    }

    public void UpdateEventLinkETag(Guid internalEventId, string? newETag)
    {
        EnsureNotDeleted();
        var link = _eventLinks.FirstOrDefault(l => l.InternalEventId == internalEventId);
        if (link == null)
        {
            throw new BusinessRuleException(Integrations_Calendar_EventLinkNotFound, $"No event link found for internal event '{internalEventId}'.");
        }
        link.UpdateETag(newETag);
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        IsActive = false;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        IncrementVersion();
        RaiseDomainEvent(new CalendarIntegrationDeactivatedDomainEvent(AccountId, WorkspaceId, Id, deletedBy, deletedAt));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        IncrementVersion();
        RaiseDomainEvent(new CalendarIntegrationActivatedDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
