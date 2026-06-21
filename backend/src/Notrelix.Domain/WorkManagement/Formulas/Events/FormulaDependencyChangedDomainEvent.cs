using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Formulas.Events;

public sealed record FormulaDependencyChangedDomainEvent(
    Guid WorkspaceId,
    Guid FormulaFieldId,
    Guid DependsOnFieldId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
