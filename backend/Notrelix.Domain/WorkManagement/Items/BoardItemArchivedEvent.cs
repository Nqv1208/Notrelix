using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items;

public sealed record BoardItemArchivedEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
