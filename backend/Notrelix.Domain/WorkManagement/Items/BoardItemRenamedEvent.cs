using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items;

public sealed record BoardItemRenamedEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid BoardId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
