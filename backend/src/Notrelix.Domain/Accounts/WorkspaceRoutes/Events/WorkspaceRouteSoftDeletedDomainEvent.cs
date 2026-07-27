namespace Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

[EventName("accounts.workspace-route-soft-deleted")]
public sealed record WorkspaceRouteSoftDeletedDomainEvent(
    Guid AccountId,
    Guid RouteId,
    string RouteSlug,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);