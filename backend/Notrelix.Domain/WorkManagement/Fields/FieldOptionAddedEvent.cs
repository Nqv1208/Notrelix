using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Fields;

public sealed record FieldOptionAddedEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid OptionId,
    string Name,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
