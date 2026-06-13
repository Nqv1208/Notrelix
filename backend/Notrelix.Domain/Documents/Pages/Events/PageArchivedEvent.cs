using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageArchivedEvent(
    Guid WorkspaceId,
    Guid PageId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
