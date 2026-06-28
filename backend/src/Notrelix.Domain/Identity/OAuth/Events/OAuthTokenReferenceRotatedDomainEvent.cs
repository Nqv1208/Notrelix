namespace Notrelix.Domain.Identity.OAuth.Events;

public sealed record OAuthTokenReferenceRotatedDomainEvent(
    Guid UserId,
    OAuthProvider Provider,
    DateTimeOffset RotatedAt
) : GlobalDomainEvent(RotatedAt);
