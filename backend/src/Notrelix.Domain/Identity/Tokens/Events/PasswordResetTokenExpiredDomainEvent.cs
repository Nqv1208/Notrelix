using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Tokens.Events;

public sealed record PasswordResetTokenExpiredDomainEvent(
    Guid TokenId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : DomainEvent(ExpiredAt, null, null);
