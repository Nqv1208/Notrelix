using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Blocks.Events;

public sealed record BlockContentUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid BlockId,
    Guid PageId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
