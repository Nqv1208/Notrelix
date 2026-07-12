namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record PasswordResetTokenRevokedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset RevokedAt
) : GlobalDomainEvent(RevokedAt);
