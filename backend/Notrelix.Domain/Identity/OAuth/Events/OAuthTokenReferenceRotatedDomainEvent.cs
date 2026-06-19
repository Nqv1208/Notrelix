using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth.Events;

public sealed record OAuthTokenReferenceRotatedDomainEvent(
    Guid UserId,
    OAuthProvider Provider,
    DateTimeOffset RotatedAt
) : DomainEvent(RotatedAt, null, null);
