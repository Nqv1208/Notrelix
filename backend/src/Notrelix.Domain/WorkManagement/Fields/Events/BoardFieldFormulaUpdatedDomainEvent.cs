namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldFormulaUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    bool IsFormula,
    string? Expression,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
