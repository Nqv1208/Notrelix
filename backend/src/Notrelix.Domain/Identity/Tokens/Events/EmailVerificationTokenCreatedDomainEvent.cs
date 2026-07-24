namespace Notrelix.Domain.Identity.Tokens.Events;

[EventName("identity.email-verification-token-created")]
public sealed record EmailVerificationTokenCreatedDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset CreatedAt
) : GlobalDomainEvent(CreatedAt);
