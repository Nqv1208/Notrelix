namespace Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

[EventName("accounts.workspace-route-set-as-default")]
public sealed record WorkspaceRouteSetAsDefaultDomainEvent(
    Guid AccountId,
    Guid RouteId,
    string RouteSlug,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);