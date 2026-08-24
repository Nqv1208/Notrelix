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
    private readonly IResourceReferenceResolver _resourceResolver;

    public LinkPageToBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider timeProvider,
        IResourceReferenceResolver resourceResolver)
    {
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
        _resourceResolver = resourceResolver;
    }

    public async Task<Result> Handle(LinkPageToBoardItemCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.BoardItems
            .FirstOrDefaultAsync(x => x.Id == request.BoardItemId, cancellationToken);

        if (card == null)
            throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var pageWorkspaceId = await _resourceResolver.GetWorkspaceIdAsync(request.PageId, ResourceTypes.Page, cancellationToken);
        if (!pageWorkspaceId.HasValue)
            throw new NotFoundException(nameof(Page), request.PageId);

        if (card.WorkspaceId != pageWorkspaceId.Value)
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
