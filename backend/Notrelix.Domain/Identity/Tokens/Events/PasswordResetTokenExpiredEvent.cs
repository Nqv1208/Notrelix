using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record PasswordResetTokenExpiredEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : DomainEvent(ExpiredAt);
