namespace Notrelix.Domain.Collaboration.Activity.Events;

public sealed record ActivityLoggedDomainEvent(
    Guid LogId,
    Guid WorkspaceId,
    ActivityType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
