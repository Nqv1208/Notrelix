using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.LinkPageToBoardItem;

[IdempotencyOperation("work-management.board-items.link-page-to-board-item.v1")]
public record LinkPageToBoardItemCommand(Guid BoardItemId, Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class LinkPageToBoardItemCommandHandler : IRequestHandler<LinkPageToBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IResourceLocator _resourceLocator;

    public LinkPageToBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider timeProvider,
        IResourceLocator resourceLocator)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _resourceLocator = resourceLocator;
    }

    public async Task<Result> Handle(LinkPageToBoardItemCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(x => x.Id == request.BoardItemId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var page = await _resourceLocator.LocateAsync(
            ResourceRef.Create(ResourceKind.Create("documents.page"), request.PageId),
            _requestContext.UserId,
            cancellationToken);
        if (page is null)
            throw new NotFoundException(nameof(Page), request.PageId);

        if (card.WorkspaceId != page.WorkspaceId)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException("CardPageWorkspaceMismatch", "BoardItem chỉ được link với page cùng workspace.");

        var now = _timeProvider.UtcNow;

        var link = BoardItemLink.Create(
            _requestContext.RequireAccountId(),
            card.WorkspaceId,
            card.BoardId,
            card.Id,
            ResourceRef.Create(ResourceKind.Create("documents.page"), request.PageId, card.WorkspaceId),
            BoardItemLinkType.Reference,
            _requestContext.UserId,
            now);

        _context.BoardItemLinks.Add(link);

        return Result.Success();
    }
}
