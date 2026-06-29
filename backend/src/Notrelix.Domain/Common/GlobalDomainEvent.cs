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
        string? causationId = null)
        : base(occurredAt, workspaceId: null, actorUserId)
    {
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
