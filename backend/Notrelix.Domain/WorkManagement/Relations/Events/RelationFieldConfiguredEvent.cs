using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record RelationFieldConfiguredEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid SourceBoardId,
    Guid TargetBoardId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
