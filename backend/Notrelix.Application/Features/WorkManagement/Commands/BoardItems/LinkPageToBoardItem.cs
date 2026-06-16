using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Application.Features.WorkManagement.Commands;

public record LinkPageToBoardItemCommand(Guid BoardItemId, Guid PageId) : IRequest<Result>;

public class LinkPageToBoardItemCommandHandler : IRequestHandler<LinkPageToBoardItemCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _timeProvider;

    public LinkPageToBoardItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(LinkPageToBoardItemCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(x => x.Id == request.BoardItemId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var page = await _context.Pages
            .FirstOrDefaultAsync(x => x.Id == request.PageId, cancellationToken);

        if (page == null)
            throw new NotFoundException(nameof(Page), request.PageId);

        await _permissions.EnsureCanEditBoardAsync(card.BoardId, _currentUser.UserId, cancellationToken);

        if (card.WorkspaceId != page.WorkspaceId)
            throw new BusinessRuleViolationException("CardPageWorkspaceMismatch", "BoardItem chỉ được link với page cùng workspace.");

        var now = new DateTimeOffset(_timeProvider.UtcNow, TimeSpan.Zero);

        var link = BoardItemLink.Create(
            card.WorkspaceId,
            card.BoardId,
            card.Id,
            ResourceRef.Create(ResourceType.Page, request.PageId, card.WorkspaceId),
            BoardItemLinkType.Reference,
            _currentUser.UserId,
            now);

        _context.BoardItemLinks.Add(link);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
