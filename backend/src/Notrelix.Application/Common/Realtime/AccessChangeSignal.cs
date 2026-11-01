using System.Text.Json;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Application.Common.Realtime;

/// <summary>
/// Tells the realtime gateway that who may see what changed, so it re-authorizes the affected subscriptions
/// now instead of at its next periodic pass. A handler calls it once after an access change succeeded; the
/// signal is enrolled in the request's outbox transaction like any realtime fact, so it is published only after
/// the change committed, on every gateway instance. The 60 s pass stays as the backstop.
/// </summary>
public interface IAccessChangeSignal
{
    /// <summary>One person lost or lowered access to one Board (member removed, role lowered, grant revoked).</summary>
    void BoardPerson(Guid boardId, Guid userId);

    /// <summary>Everyone's access to one Board may have changed (visibility, a Team grant).</summary>
    void Board(Guid boardId);

    /// <summary>One person's access across the workspace may have changed (member removed or suspended, Space or Team left).</summary>
    void WorkspacePerson(Guid workspaceId, Guid userId);

    /// <summary>Several people's access across the workspace may have changed (a Team or a Space lost its entry).</summary>
    void Workspace(Guid workspaceId);
}

public sealed class AccessChangeSignal : IAccessChangeSignal
{
    private static readonly JsonElement EmptyPayload = JsonSerializer.SerializeToElement(new { });

    private readonly IIntegrationEventCollector _events;
    private readonly IExecutionContextReader _context;
    private readonly IDateTimeProvider _time;

    public AccessChangeSignal(IIntegrationEventCollector events, IExecutionContextReader context, IDateTimeProvider time)
    {
        _events = events;
        _context = context;
        _time = time;
    }

    public void BoardPerson(Guid boardId, Guid userId) => Add(RealtimeAccessSignal.BoardResource, boardId, userId);

    public void Board(Guid boardId) => Add(RealtimeAccessSignal.BoardResource, boardId, null);

    public void WorkspacePerson(Guid workspaceId, Guid userId) => Add(RealtimeAccessSignal.WorkspaceResource, workspaceId, userId, workspaceId);

    public void Workspace(Guid workspaceId) => Add(RealtimeAccessSignal.WorkspaceResource, workspaceId, null, workspaceId);

    private Guid WorkspaceId() => _context.WorkspaceId ?? throw new InvalidOperationException("An access signal needs the workspace of the request.");

    private void Add(string resourceKind, Guid resourceId, Guid? subjectUserId, Guid? workspace = null)
    {
        var now = _time.UtcNow;
        var workspaceId = workspace ?? WorkspaceId();
        // The subject rides in the payload; the gateway reads it back as the signal's SubjectUserId.
        var payload = JsonSerializer.SerializeToElement(new { userId = subjectUserId });
        _events.Add(new RealtimeResourceChangedV1(
            Guid.CreateVersion7(),
            _context.AccountId,
            workspaceId,
            _context.UserId,
            _context.CorrelationId == Guid.Empty ? Guid.NewGuid() : _context.CorrelationId,
            _context.CausationId,
            now,
            RealtimeAccessSignal.TopicNamespace,
            resourceKind,
            resourceId,
            $"{RealtimeAccessSignal.TopicNamespace}:{resourceKind}:{resourceId:N}",
            RealtimeStreamClock.Next(now),
            RealtimeAccessSignal.ChangeKind,
            RealtimeAccessSignal.PayloadContract,
            subjectUserId is null ? EmptyPayload : payload));
    }
}
