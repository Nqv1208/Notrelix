using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Documents.Templates.Events;

public sealed record PageTemplateCreatedEvent(
    Guid? WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
