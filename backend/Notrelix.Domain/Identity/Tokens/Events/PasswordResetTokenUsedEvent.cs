using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record PasswordResetTokenUsedEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset UsedAt
) : DomainEvent(UsedAt, null, null);
