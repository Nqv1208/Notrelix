using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Blocks.Events;

public sealed record BlockSoftDeletedEvent(
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
