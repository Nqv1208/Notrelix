using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;

[IdempotencyOperation("work-management.board-items.unlink-page-from-board-item.v1")]
public record UnlinkPageFromBoardItemCommand(Guid BoardItemId, Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class UnlinkPageFromBoardItemCommandHandler : IRequestHandler<UnlinkPageFromBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UnlinkPageFromBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UnlinkPageFromBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        // Only this page's link: item-to-item links and other pages stay. Unlinking a page that is not linked is a no-op.
        var pageKind = ResourceKind.Create("documents.page");
        var existingLinks = (await _context.BoardItemLinks
                .Where(l => l.SourceItemId == card.Id)
                .ToListAsync(ct))
            .Where(l => l.Target.Kind == pageKind && l.Target.ResourceId == request.PageId)
            .ToList();
        _context.BoardItemLinks.RemoveRange(existingLinks);

        return Result.Success();
    }
}
