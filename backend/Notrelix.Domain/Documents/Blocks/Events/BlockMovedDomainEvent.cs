using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Blocks.Events;

public sealed record BlockMovedDomainEvent(
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid? OldParentId,
    Guid? NewParentId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
