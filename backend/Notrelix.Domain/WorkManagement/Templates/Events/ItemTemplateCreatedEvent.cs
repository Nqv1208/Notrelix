using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Templates.Events;

public sealed record ItemTemplateCreatedEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
