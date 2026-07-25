namespace Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

[EventName("accounts.workspace-route-unlinked")]
public sealed record WorkspaceRouteUnlinkedDomainEvent(
    Guid AccountId,
    Guid RouteId,
    string RouteSlug,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);