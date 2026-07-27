namespace Notrelix.Domain.Identity.OAuth.Events;

[EventName("identity.o-auth-profile-updated")]
public sealed record OAuthProfileUpdatedDomainEvent(
    Guid UserId,
    OAuthProvider Provider,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);