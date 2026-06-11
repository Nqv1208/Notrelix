using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Credentials;

public sealed record PasswordResetRequestedEvent(
    Guid UserId,
    string Email,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
