using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items;

public sealed record BoardItemLabelAddedEvent(
    Guid WorkspaceId,
    Guid ItemId,
    Guid LabelId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
