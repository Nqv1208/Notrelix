using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.ShareLinks;

public sealed record ShareLinkRotatedEvent(
    Guid WorkspaceId,
    Guid LinkId,
    Guid RotatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
