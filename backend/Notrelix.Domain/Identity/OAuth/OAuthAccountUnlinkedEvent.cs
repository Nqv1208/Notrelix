using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Identity.OAuth;

public sealed record OAuthAccountUnlinkedEvent(
    Guid UserId,
    OAuthProvider Provider,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
