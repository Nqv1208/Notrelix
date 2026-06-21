using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.ResourceLinks.Events;

public sealed record ResourceLinkCreatedDomainEvent(
    Guid WorkspaceId,
    Guid SourceId,
    Guid TargetId,
    LinkType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
