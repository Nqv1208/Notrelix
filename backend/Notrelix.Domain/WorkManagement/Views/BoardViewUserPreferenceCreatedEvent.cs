using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed record BoardViewUserPreferenceCreatedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    Guid UserId,
    Guid PreferenceId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
