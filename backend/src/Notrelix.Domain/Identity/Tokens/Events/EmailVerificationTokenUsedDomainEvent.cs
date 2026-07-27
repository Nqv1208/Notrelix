namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.email-verification-token-used")]
public sealed record EmailVerificationTokenUsedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset UsedAt
) : GlobalDomainEvent(UsedAt);
