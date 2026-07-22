namespace Notrelix.Domain.Integrations.Calendar.Events;

public sealed record CalendarIntegrationConnectedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid ConnectionId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
