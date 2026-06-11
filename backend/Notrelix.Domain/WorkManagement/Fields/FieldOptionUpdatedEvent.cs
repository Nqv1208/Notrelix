using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Fields;

public sealed record FieldOptionUpdatedEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
