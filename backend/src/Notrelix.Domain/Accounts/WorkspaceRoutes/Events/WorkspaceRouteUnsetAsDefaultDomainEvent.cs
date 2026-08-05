namespace Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

[EventName("accounts.workspace-route-unset-as-default")]
public sealed record WorkspaceRouteUnsetAsDefaultDomainEvent(
    Guid AccountId,
    Guid RouteId,
    string RouteSlug,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);