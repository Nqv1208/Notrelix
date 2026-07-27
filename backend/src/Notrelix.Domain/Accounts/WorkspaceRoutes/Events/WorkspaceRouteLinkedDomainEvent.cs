namespace Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

[EventName("accounts.workspace-route-linked")]
public sealed record WorkspaceRouteLinkedDomainEvent(
    Guid AccountId,
    Guid RouteId,
    string RouteSlug,
    Guid WorkspaceId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);