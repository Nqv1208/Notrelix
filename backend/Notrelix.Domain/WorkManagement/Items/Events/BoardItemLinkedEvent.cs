using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Items.Events;

public sealed record BoardItemLinkedEvent(
    Guid WorkspaceId,
    Guid SourceItemId,
    ResourceRef Target,
    BoardItemLinkType LinkType,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
