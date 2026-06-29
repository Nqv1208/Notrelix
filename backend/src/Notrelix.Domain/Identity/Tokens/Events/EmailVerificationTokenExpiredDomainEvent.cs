namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record EmailVerificationTokenExpiredDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : GlobalDomainEvent(ExpiredAt);
