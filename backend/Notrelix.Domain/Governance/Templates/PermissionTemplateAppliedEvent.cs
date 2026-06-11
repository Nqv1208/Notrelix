using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Governance.Templates;

public sealed record PermissionTemplateAppliedEvent(
    Guid WorkspaceId,
    Guid TemplateId,
    Guid TargetResourceId,
    Guid AppliedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
