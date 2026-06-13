using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Views.Events;

public sealed record SavedFilterCreatedEvent(
    Guid FilterId,
    Guid WorkspaceId,
    Guid BoardId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt,
    Guid? ViewId = null
) : DomainEvent(OccurredAt);
