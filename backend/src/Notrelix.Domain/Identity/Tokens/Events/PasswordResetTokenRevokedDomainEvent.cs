namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.password-reset-token-revoked")]
public sealed record PasswordResetTokenRevokedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset RevokedAt
) : GlobalDomainEvent(RevokedAt);
