namespace Notrelix.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public int EventVersion { get; init; } = 1;

    private readonly Guid? _workspaceId;
    private readonly Guid? _actorUserId;

    Guid? IDomainEvent.WorkspaceId => _workspaceId;
    Guid? IDomainEvent.ActorUserId => _actorUserId;

    protected DomainEvent(DateTimeOffset occurredAt, Guid? workspaceId = null, Guid? actorUserId = null)
    {
        EventId = Guid.CreateVersion7();
        OccurredAt = occurredAt;
        _workspaceId = workspaceId;
        _actorUserId = actorUserId;
    }
}
