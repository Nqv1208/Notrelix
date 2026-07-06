using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;

public record UnlinkPageFromBoardItemCommand(Guid BoardItemId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
}

public class UnlinkPageFromBoardItemCommandHandler : IRequestHandler<UnlinkPageFromBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;

    public UnlinkPageFromBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    public async Task<Result> Handle(UnlinkPageFromBoardItemCommand request, CancellationToken ct)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId, ct);
        if (card is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        await _permissions.EnsureCanEditBoardAsync(card.BoardId, _currentUser.UserId, ct);

        var existingLinks = await _context.BoardItemLinks
            .Where(l => l.SourceItemId == card.Id)
            .ToListAsync(ct);
        _context.BoardItemLinks.RemoveRange(existingLinks);

        return Result.Success();
    }
}
