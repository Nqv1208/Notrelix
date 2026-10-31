using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Ports.Realtime;
using Notrelix.Application.Features.WorkManagement.Public.BoardRealtime;

namespace Notrelix.Application.Features.WorkManagement.Realtime;

public sealed class BoardRealtimeNotifier : IBoardRealtimeNotifier
{
    private const string BoardKind = "work-management.board";
    private static readonly JsonElement EmptyPayload = JsonSerializer.SerializeToElement(new { });

    private readonly IBoardRealtimeScopeFacts _scope;
    private readonly IIntegrationEventCollector _events;
    private readonly IExecutionContextReader _context;
    private readonly IDateTimeProvider _time;

    public BoardRealtimeNotifier(
        IBoardRealtimeScopeFacts scope,
        IIntegrationEventCollector events,
        IExecutionContextReader context,
        IDateTimeProvider time)
    {
        _scope = scope;
        _events = events;
        _context = context;
        _time = time;
    }

    public Task NotifyAsync(object request, object? response, CancellationToken cancellationToken)
    {
        if (request is not IRequirePermission { Resource: var resource })
            throw new InvalidOperationException($"{request.GetType().Name} names no resource to notify.");

        var name = request.GetType().Name;
        var changeKind = name.EndsWith("Command", StringComparison.Ordinal) ? name[..^"Command".Length] : name;
        return NotifyResourceAsync(resource.Kind.Value, resource.ResourceId, changeKind, response, cancellationToken);
    }

    public async Task NotifyResourceAsync(string resourceKind, Guid resourceId, string changeKind, object? response, CancellationToken cancellationToken)
    {
        if (response is Result { Succeeded: false }) return;
        if (_scope is not null && !_scope.HasPendingChanges()) return;

        var boardId = resourceKind == BoardKind
            ? resourceId
            : await _scope.ResolveBoardIdAsync(resourceKind, resourceId, cancellationToken);
        if (boardId is { } id) await NotifyBoardAsync(id, changeKind, cancellationToken);
    }

    public Task NotifyBoardAsync(Guid id, string changeKind, CancellationToken cancellationToken) =>
        NotifyBoardAsync(
            id, changeKind, _context.AccountId, _context.WorkspaceId, _context.UserId,
            _context.CorrelationId, _context.CausationId, cancellationToken);

    public Task NotifyBoardAsync(
        Guid id, string changeKind, Guid? accountId, Guid? workspaceId, Guid? actorUserId,
        Guid? correlationId, Guid? causationId, CancellationToken cancellationToken)
    {
        var now = _time.UtcNow;
        _events.Add(new RealtimeResourceChangedV1(
            Guid.CreateVersion7(),
            accountId,
            workspaceId,
            actorUserId,
            correlationId is { } c && c != Guid.Empty ? c : Guid.NewGuid(),
            causationId,
            now,
            "board",
            "Board",
            id,
            $"board:Board:{id:N}",
            RealtimeStreamClock.Next(now),
            changeKind,
            "board.changed.v1",
            EmptyPayload));
        return Task.CompletedTask;
    }
}
