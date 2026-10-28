using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Ports.Realtime;

namespace Notrelix.Application.Features.WorkManagement.Realtime;

public sealed class BoardRealtimeNotifier : IBoardRealtimeNotifier
{
    private const string BoardKind = "work-management.board";
    private static readonly JsonElement EmptyPayload = JsonSerializer.SerializeToElement(new { });

    private readonly IBoardRealtimeScopeFacts? _scope;
    private readonly IIntegrationEventCollector? _events;
    private readonly IExecutionContextReader _context;
    private readonly IDateTimeProvider _time;

    public BoardRealtimeNotifier(
        IExecutionContextReader context,
        IDateTimeProvider time,
        IBoardRealtimeScopeFacts? scope = null,
        IIntegrationEventCollector? events = null)
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

        var boardId = resourceKind == BoardKind
            ? resourceId
            : _scope is null ? null : await _scope.ResolveBoardIdAsync(resourceKind, resourceId, cancellationToken);
        if (boardId is { } id) await NotifyBoardAsync(id, changeKind, cancellationToken);
    }

    public Task NotifyBoardAsync(Guid id, string changeKind, CancellationToken cancellationToken)
    {
        var correlationId = _context.CorrelationId == Guid.Empty ? Guid.NewGuid() : _context.CorrelationId;
        var now = _time.UtcNow;
        // Hosts that collect no integration events (partial test graphs) announce nothing.
        _events?.Add(new RealtimeResourceChangedV1(
            Guid.CreateVersion7(),
            _context.AccountId,
            _context.WorkspaceId,
            _context.UserId,
            correlationId,
            _context.CausationId,
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
