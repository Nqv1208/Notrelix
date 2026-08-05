namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.password-reset-token-expired")]
public sealed record PasswordResetTokenExpiredDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : GlobalDomainEvent(ExpiredAt);
