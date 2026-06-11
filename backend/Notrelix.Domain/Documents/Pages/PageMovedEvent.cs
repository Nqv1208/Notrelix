using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Pages;

public sealed record PageMovedEvent(
    Guid WorkspaceId,
    Guid PageId,
    Guid? OldParentId,
    Guid? NewParentId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
