using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.ResourceLinks;

public sealed record ResourceLinkDeletedEvent(
    Guid WorkspaceId,
    Guid LinkId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
