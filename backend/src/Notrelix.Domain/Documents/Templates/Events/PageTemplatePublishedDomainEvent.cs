using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Templates.Events;

public sealed record PageTemplatePublishedDomainEvent(
    Guid? WorkspaceId,
    Guid TemplateId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
