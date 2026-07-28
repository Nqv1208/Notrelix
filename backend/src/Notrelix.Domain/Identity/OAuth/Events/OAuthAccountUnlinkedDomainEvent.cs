namespace Notrelix.Domain.Identity.OAuth.Events;

[EventName("identity.o-auth-account-unlinked")]
public sealed record OAuthAccountUnlinkedDomainEvent(
    Guid UserId,
    OAuthProvider Provider,
    string ProviderId,
    Guid UnlinkedBy,
    DateTimeOffset UnlinkedAt
) : GlobalDomainEvent(UnlinkedAt);
