using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Versions;

public sealed record DocumentVersionRestoredEvent(
    Guid WorkspaceId,
    Guid PageId,
    int VersionNumber,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
