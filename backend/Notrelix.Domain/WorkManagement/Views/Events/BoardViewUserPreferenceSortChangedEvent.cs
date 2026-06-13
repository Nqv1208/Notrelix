using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record BoardViewUserPreferenceSortChangedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    Guid UserId,
    Guid PreferenceId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
