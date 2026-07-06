namespace Notrelix.Domain.WorkManagement.Formulas.Events;

public sealed record FormulaDependencyChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FormulaFieldId,
    Guid DependsOnFieldId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
