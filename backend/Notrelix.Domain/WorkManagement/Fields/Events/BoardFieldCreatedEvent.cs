using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldCreatedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid FieldId,
    string Name,
    FieldType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
