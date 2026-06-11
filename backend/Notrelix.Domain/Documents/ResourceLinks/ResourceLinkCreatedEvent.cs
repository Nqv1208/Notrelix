using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.ResourceLinks;

public sealed record ResourceLinkCreatedEvent(
    Guid WorkspaceId,
    Guid SourceId,
    Guid TargetId,
    LinkType Type,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
