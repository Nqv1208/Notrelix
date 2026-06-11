using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items;

public sealed record BoardItemCreatedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid GroupId,
    Guid ItemId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
