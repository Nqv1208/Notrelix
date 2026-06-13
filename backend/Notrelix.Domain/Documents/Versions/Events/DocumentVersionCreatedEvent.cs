using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Versions.Events;

public sealed record DocumentVersionCreatedEvent(
    Guid WorkspaceId,
    Guid PageId,
    int VersionNumber,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
