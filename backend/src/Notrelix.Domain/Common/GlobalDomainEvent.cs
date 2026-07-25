namespace Notrelix.Domain.Common;

/// <summary>
/// Base for events that occur outside any workspace scope.
/// Examples: UserRegistered, PlanCreated, SystemSettingChanged.
/// Must NOT carry a required WorkspaceId.
/// </summary>
public abstract record GlobalDomainEvent : DomainEvent
{
    protected GlobalDomainEvent(
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null,
        Guid subjectId = default)
        : base(occurredAt, workspaceId: null, actorUserId, subjectId)
    {
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
