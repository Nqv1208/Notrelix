using Notrelix.Domain.Common;
using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.WorkManagement;

[EventName("board.item.field_value.changed", Version = 1)]
public sealed record BoardItemFieldValueChangedIntegrationEvent(
    Guid ItemId,
    Guid BoardId,
    Guid FieldId,
    Guid? WorkspaceId,
    string? OldValue,
    string? NewValue,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "board.item.field_value.changed",
    1,
    sourceEventId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
