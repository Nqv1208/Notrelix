namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record PasswordResetTokenUsedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset UsedAt
) : DomainEvent(UsedAt, null, null);
