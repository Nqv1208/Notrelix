using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items;

public sealed record BoardItemLabelRemovedEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid LabelId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
