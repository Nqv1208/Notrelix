using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.MoveBoardGroup;

/// <summary>
/// Normal group drag (WM-V3-APP-GROUP-001): moves one group relative to its
/// neighbors under the board's group ordering lock, rewriting only its own key.
/// Replaces the full-list ReorderBoardGroups for drag.
/// </summary>
[IdempotencyOperation("work-management.board-groups.move-board-group.v1")]
public record MoveBoardGroupCommand(Guid GroupId, long ExpectedVersion, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    // Moving a group edits Board content (Editor), not Board management.
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-group"), GroupId);
}

public class MoveBoardGroupCommandHandler : IRequestHandler<MoveBoardGroupCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardRealtimeNotifier? _realtime;

    public MoveBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result> Handle(MoveBoardGroupCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(MoveBoardGroupCommand request, CancellationToken ct)
    {
        var group = await _context.BoardGroups.FirstOrDefaultAsync(g => g.Id == request.GroupId, ct)
            ?? throw new NotFoundException(nameof(BoardGroup), request.GroupId);

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.BoardGroups, group.BoardId),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => BoardGroupSiblings.LoadAsync(_context, group.BoardId, token),
            ct,
            movedEntityId: group.Id);

        group.UpdatePosition(position, _requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
