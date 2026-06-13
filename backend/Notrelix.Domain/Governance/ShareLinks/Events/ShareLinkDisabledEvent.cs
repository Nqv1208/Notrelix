using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.ShareLinks.Events;

public sealed record ShareLinkDisabledEvent(
    Guid WorkspaceId,
    Guid LinkId,
    Guid DisabledBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
