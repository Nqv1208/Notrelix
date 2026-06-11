using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed record BoardViewCreatedEvent(
    Guid WorkspaceId,
    Guid BoardId,
    Guid ViewId,
    string Name,
    ViewType Type,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
