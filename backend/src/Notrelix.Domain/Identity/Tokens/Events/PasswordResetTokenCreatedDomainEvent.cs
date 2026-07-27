namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.password-reset-token-created")]
public sealed record PasswordResetTokenCreatedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset CreatedAt
) : GlobalDomainEvent(CreatedAt);
