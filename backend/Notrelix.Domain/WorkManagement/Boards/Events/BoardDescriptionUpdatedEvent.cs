using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Boards.Events;

public sealed record BoardDescriptionUpdatedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    string? OldDescription,
    string? NewDescription,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
