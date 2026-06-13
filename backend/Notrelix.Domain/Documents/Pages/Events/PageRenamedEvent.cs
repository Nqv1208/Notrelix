using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageRenamedEvent(
    Guid WorkspaceId,
    Guid PageId,
    string OldTitle,
    string NewTitle,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
