using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Templates;

public sealed record PermissionTemplateCreatedEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
