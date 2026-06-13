using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.OAuth.Events;

public sealed record OAuthAccountLinkedEvent(
    Guid UserId,
    OAuthProvider Provider,
    string ProviderId,
    DateTimeOffset LinkedAt
) : DomainEvent(LinkedAt);
