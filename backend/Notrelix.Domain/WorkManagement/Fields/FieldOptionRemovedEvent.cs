using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Fields;

public sealed record FieldOptionRemovedEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
