using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Blocks;

public sealed record BlockMovedEvent(
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid? OldParentId,
    Guid? NewParentId,
    string NewPosition,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
