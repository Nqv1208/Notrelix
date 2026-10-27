using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemTrash;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RestoreBoardItem;

[IdempotencyOperation("work-management.board-items.restore-board-item.v1")]
public record RestoreBoardItemCommand(Guid BoardItemId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class RestoreBoardItemCommandHandler : IRequestHandler<RestoreBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;

    public RestoreBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result> Handle(RestoreBoardItemCommand request, CancellationToken ct)
    {
        // The global filter hides soft-deleted rows, and on EF Core 9 bypassing it also
        // drops its tenant predicate, so the resolved tenant scope is re-applied here.
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var item = await _context.BoardItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.AccountId == accountId && i.WorkspaceId == workspaceId, ct);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        if (item.DeletedAt is { } deletedAt && _dateTimeProvider.UtcNow - deletedAt > BoardItemTrashPolicy.RestoreWindow)
            throw new ConflictException("This item was deleted more than 30 days ago and can no longer be restored.", "work-management.item.restore-window-closed");

        // A deleted root item's key may have been reissued to an active sibling;
        // restore keeps the key when free, otherwise lands right after its holder.
        FractionalIndex? replacement = null;
        if (item.IsDeleted && item.ParentItemId is null)
        {
            replacement = await _placement.ResolveRestoreAsync(
                new WorkOrderingScope(WorkOrderingScopeKind.GroupItems, item.GroupId),
                item.Position,
                token => GroupItemSiblings.LoadAsync(_context, item.GroupId, token),
                ct);
        }

        item.Restore(_requestContext.UserId, _dateTimeProvider.UtcNow, replacement);
        return Result.Success();
    }
}
