using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth.Events;

public sealed record OAuthAccountUnlinkedDomainEvent(
    Guid UserId,
    OAuthProvider Provider,
    string ProviderId,
    DateTimeOffset UnlinkedAt
) : DomainEvent(UnlinkedAt, null, null);
