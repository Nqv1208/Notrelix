using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Versions.Events;

public sealed record DocumentVersionRestoredEvent(
    Guid WorkspaceId,
    Guid PageId,
    int VersionNumber,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
