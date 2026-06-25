namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record EmailVerificationTokenCreatedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset CreatedAt
) : DomainEvent(CreatedAt, null, null);
