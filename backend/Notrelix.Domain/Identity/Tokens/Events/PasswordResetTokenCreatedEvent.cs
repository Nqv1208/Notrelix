using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record PasswordResetTokenCreatedEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset CreatedAt
) : DomainEvent(CreatedAt, null, null);
