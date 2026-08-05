namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.password-reset-token-used")]
public sealed record PasswordResetTokenUsedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset UsedAt
) : GlobalDomainEvent(UsedAt);
