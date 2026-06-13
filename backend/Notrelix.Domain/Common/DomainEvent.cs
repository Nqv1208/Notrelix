using System.Collections.Concurrent;
using System.Reflection;

namespace Notrelix.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> WorkspaceIdProperties = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> ActorUserIdProperties = new();

    private readonly Guid? _workspaceId;
    private readonly Guid? _actorUserId;

    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }
    
    Guid? IDomainEvent.WorkspaceId
    {
        get
        {
            if (_workspaceId.HasValue && _workspaceId.Value != Guid.Empty) return _workspaceId.Value;
            var type = GetType();
            var prop = WorkspaceIdProperties.GetOrAdd(type, t => t.GetProperty("WorkspaceId"));
            if (prop != null)
            {
                var val = prop.GetValue(this);
                if (val is Guid g) return g;
            }
            return null;
        }
    }

    Guid? IDomainEvent.ActorUserId
    {
        get
        {
            if (_actorUserId.HasValue && _actorUserId.Value != Guid.Empty) return _actorUserId.Value;
            var type = GetType();
            var prop = ActorUserIdProperties.GetOrAdd(type, t => 
                t.GetProperty("ActorUserId") ?? 
                t.GetProperty("UserId") ?? 
                t.GetProperty("CreatedBy") ?? 
                t.GetProperty("UpdatedBy") ??
                t.GetProperty("ActorId"));
            if (prop != null)
            {
                var val = prop.GetValue(this);
                if (val is Guid g) return g;
            }
            return null;
        }
    }

    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public int EventVersion { get; init; } = 1;

    protected DomainEvent(DateTimeOffset occurredAt, Guid? workspaceId = null, Guid? actorUserId = null)
    {
        EventId = Guid.CreateVersion7();
        OccurredAt = occurredAt;
        _workspaceId = workspaceId;
        _actorUserId = actorUserId;
    }
}
