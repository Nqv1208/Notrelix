using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Templates;

public sealed record BoardTemplateCreatedEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
