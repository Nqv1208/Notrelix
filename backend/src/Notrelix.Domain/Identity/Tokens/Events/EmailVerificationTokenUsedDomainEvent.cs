namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record EmailVerificationTokenUsedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset UsedAt
) : GlobalDomainEvent(UsedAt);
