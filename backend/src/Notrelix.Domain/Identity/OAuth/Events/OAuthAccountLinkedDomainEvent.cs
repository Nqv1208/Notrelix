namespace Notrelix.Domain.Identity.OAuth.Events;

[EventName("identity.o-auth-account-linked")]
public sealed record OAuthAccountLinkedDomainEvent(
    Guid UserId,
    OAuthProvider Provider,
    string ProviderId,
    DateTimeOffset LinkedAt
) : GlobalDomainEvent(LinkedAt);
