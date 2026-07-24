namespace Notrelix.Domain.Identity.OAuth.Events;

[EventName("identity.o-auth-token-reference-rotated")]
public sealed record OAuthTokenReferenceRotatedDomainEvent(
    Guid UserId,
    OAuthProvider Provider,
    DateTimeOffset RotatedAt
) : GlobalDomainEvent(RotatedAt);
