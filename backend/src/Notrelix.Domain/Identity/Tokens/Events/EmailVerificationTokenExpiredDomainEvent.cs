namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.email-verification-token-expired")]
public sealed record EmailVerificationTokenExpiredDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : GlobalDomainEvent(ExpiredAt);
