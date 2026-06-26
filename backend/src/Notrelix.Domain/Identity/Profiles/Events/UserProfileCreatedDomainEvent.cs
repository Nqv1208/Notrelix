namespace Notrelix.Domain.Identity.Profiles.Events;

public sealed record UserProfileCreatedDomainEvent(
    Guid UserProfileId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, workspaceId: null, UserId);
