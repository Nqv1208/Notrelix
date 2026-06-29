namespace Notrelix.Domain.Identity.OAuth.Events;

public sealed record OAuthAccountLinkedDomainEvent(
    Guid UserId,
    OAuthProvider Provider,
    string ProviderId,
    DateTimeOffset LinkedAt
) : GlobalDomainEvent(LinkedAt);
