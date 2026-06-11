using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record EmailVerificationTokenCreatedEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset CreatedAt
) : DomainEvent(CreatedAt);
