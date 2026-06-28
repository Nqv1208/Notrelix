namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record PasswordResetTokenExpiredDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : GlobalDomainEvent(ExpiredAt);
