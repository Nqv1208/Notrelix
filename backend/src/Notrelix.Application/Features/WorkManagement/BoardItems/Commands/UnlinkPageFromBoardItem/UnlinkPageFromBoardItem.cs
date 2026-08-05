using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;

[IdempotencyOperation("work-management.board-items.unlink-page-from-board-item.v1")]
public record UnlinkPageFromBoardItemCommand(Guid BoardItemId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
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

        var existingLinks = await _context.BoardItemLinks
            .Where(l => l.SourceItemId == card.Id)
            .ToListAsync(ct);
        _context.BoardItemLinks.RemoveRange(existingLinks);

        return Result.Success();
    }
}
