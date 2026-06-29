namespace Notrelix.Domain.WorkManagement.Formulas.Events;

public sealed record FormulaDependencyChangedDomainEvent(
    Guid WorkspaceId,
    Guid FormulaFieldId,
    Guid DependsOnFieldId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
