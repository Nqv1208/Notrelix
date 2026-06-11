using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.ShareLinks;

public sealed record ShareLinkExpiredEvent(
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
