namespace Notrelix.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }
    public string SourceContext { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
    public Guid AggregateId { get; init; }
    public string SubjectType { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public int EventVersion { get; init; } = 1;

    private readonly Guid? _workspaceId;
    private readonly Guid? _actorUserId;
    private readonly Guid _subjectId;

    Guid? IDomainEvent.WorkspaceId => _workspaceId;
    Guid? IDomainEvent.ActorUserId => _actorUserId;
    Guid IDomainEvent.SubjectId => _subjectId;

    protected DomainEvent(DateTimeOffset occurredAt, Guid? workspaceId = null, Guid? actorUserId = null, Guid subjectId = default)
    {
        EventId = Guid.CreateVersion7();
        OccurredAt = occurredAt;
        _workspaceId = workspaceId;
        _actorUserId = actorUserId;
        _subjectId = subjectId;
    }
}
