namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record EmailVerificationTokenRevokedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset RevokedAt
) : GlobalDomainEvent(RevokedAt);
