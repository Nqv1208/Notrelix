namespace Notrelix.Domain.Accounts.WorkspaceRoutes.Events;

[EventName("accounts.workspace-route-created")]
public sealed record WorkspaceRouteCreatedDomainEvent(
    Guid AccountId,
    Guid RouteId,
    string RouteSlug,
    Guid? WorkspaceId,
    bool IsDefault,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : AccountScopedDomainEvent(AccountId, OccurredAt);