using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldFormulaUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    bool IsFormula,
    string? Expression,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
