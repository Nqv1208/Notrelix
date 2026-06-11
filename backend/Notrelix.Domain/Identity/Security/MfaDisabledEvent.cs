using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.Security;

public sealed record MfaDisabledEvent(
    Guid UserId,
    MfaMethodType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
