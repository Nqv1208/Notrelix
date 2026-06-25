namespace Notrelix.Domain.Billing.Usage.Events;

public sealed record WorkspaceFeatureUsageResetDomainEvent(
    Guid WorkspaceId,
    FeatureCode Feature,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
