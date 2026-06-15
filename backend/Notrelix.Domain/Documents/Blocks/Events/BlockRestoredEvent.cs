using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Blocks.Events;

public sealed record BlockRestoredEvent(
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
