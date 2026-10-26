namespace Notrelix.Application.Events.Documents;

/// <summary>A person a saved block newly mentions.</summary>
public sealed record PageBlockMention(Guid BlockId, Guid UserId);

/// <summary>
/// The blocks of a page were saved: the reactions that follow a committed edit (an automatic history entry, a
/// notification to the people the edit newly mentions) consume this. It carries the mentions the save added and
/// nothing of the content itself.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("page.blocks-saved", Version = 1)]
public sealed record PageBlocksSavedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid PageId,
    Guid? WorkspaceId,
    string PageTitle,
    IReadOnlyList<PageBlockMention> Mentions,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "page.blocks-saved",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
