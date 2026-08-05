namespace Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

[EventName("accounts.workspace-route-deleted")]
public sealed record WorkspaceRouteDeletedDomainEvent(
    Guid AccountId,
    Guid RouteId,
    string RouteSlug,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : AccountScopedDomainEvent(AccountId, OccurredAt);
