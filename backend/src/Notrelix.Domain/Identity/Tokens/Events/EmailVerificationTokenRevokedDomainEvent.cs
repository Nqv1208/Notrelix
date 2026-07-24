namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.email-verification-token-revoked")]
public sealed record EmailVerificationTokenRevokedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset RevokedAt
) : GlobalDomainEvent(RevokedAt);
