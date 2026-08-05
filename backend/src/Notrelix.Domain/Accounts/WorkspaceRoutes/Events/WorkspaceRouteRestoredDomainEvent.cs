namespace Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

[EventName("accounts.workspace-route-restored")]
public sealed record WorkspaceRouteRestoredDomainEvent(
    Guid AccountId,
    Guid RouteId,
    string RouteSlug,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);