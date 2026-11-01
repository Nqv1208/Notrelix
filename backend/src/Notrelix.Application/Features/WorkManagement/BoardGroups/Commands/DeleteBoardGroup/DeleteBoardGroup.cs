using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;

[IdempotencyOperation("work-management.board-groups.delete-board-group.v1")]
public record DeleteBoardGroupCommand(Guid GroupId, long ExpectedVersion, Guid? TargetGroupId = null)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    // Deleting a group edits Board content (Editor), not Board management.
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-group"), GroupId);
}

/// <summary>
/// Deletes a group. An empty group goes without a target; a group that has items needs a
/// target group of the same board, which receives the items after its own, in their order.
/// The last group of a board cannot be deleted.
/// </summary>
public class DeleteBoardGroupCommandHandler : IRequestHandler<DeleteBoardGroupCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;
    private readonly IBoardRealtimeNotifier? _realtime;

    public DeleteBoardGroupCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result> Handle(DeleteBoardGroupCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(DeleteBoardGroupCommand request, CancellationToken ct)
    {
        var group = await _context.BoardGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, ct);
        if (group is null) throw new NotFoundException(nameof(BoardGroup), request.GroupId);
        if (group.IsDeleted) return Result.Success();

        var activeGroups = await _context.BoardGroups
            .CountAsync(g => g.BoardId == group.BoardId && g.DeletedAt == null, ct);
        group.EnsureNotLastGroup(activeGroups);

        var now = _dateTimeProvider.UtcNow;
        var items = await _context.BoardItems
            .Where(item => item.GroupId == group.Id && item.DeletedAt == null)
            .ToListAsync(ct);

        if (items.Count > 0)
        {
            if (request.TargetGroupId is not { } targetId)
                return Result.Failure(new ApplicationError(
                    WorkManagementRuleCodes.WorkManagement_Group_ItemsNeedTargetGroup,
                    "A group that has items can only be deleted together with a target group for them.",
                    ApplicationErrorType.BusinessRule));

            var target = await _context.BoardGroups
                .FirstOrDefaultAsync(g => g.Id == targetId && g.BoardId == group.BoardId, ct);
            if (target is null)
                return Result.Failure(new ApplicationError(
                    WorkManagementRuleCodes.WorkManagement_Group_InvalidTargetGroup,
                    "The target group must be another group of the same board.",
                    ApplicationErrorType.BusinessRule));
            target.EnsureCanReceiveItemsOf(group);

            await MoveItemsAsync(items, target, now, ct);
        }

        group.Delete(_currentUser.UserId, now);
        return Result.Success();
    }

    private async Task MoveItemsAsync(List<BoardItem> items, BoardGroup target, DateTimeOffset now, CancellationToken ct)
    {
        var targetRef = BoardGroupRef.From(target);
        var roots = items.Where(item => item.ParentItemId is null).OrderBy(item => item.Position).ThenBy(item => item.Id).ToList();

        // Roots go after the target's last item, keeping their order; the scope lock is taken once.
        if (roots.Count > 0)
        {
            var first = await _placement.ResolveAsync(
                new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, target.Id),
                WorkPlacementIntent.Append,
                token => GroupItemSiblings.LoadAsync(_context, target.Id, token),
                ct);
            var rest = roots.Count > 1
                ? FractionalIndexGenerator.GenerateNKeysBetween(first, null, roots.Count - 1)
                : [];
            for (var i = 0; i < roots.Count; i++)
                roots[i].MoveToGroupOnGroupRemoval(targetRef, i == 0 ? first : rest[i - 1], _currentUser.UserId, now);
        }

        // Child items follow their parent's group and keep their place under it.
        foreach (var child in items.Where(item => item.ParentItemId is not null))
            child.MoveToGroupOnGroupRemoval(targetRef, null, _currentUser.UserId, now);
    }
}
