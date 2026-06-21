using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record FieldOptionAddedDomainEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    string Name,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, AddedBy);
