using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.LinkPageToBoardItem;

public record LinkPageToBoardItemCommand(Guid BoardItemId, Guid PageId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemId);
}

public class LinkPageToBoardItemCommandHandler : IRequestHandler<LinkPageToBoardItemCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspacePermissionService _permissions;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IResourceReferenceResolver _resourceResolver;
    private readonly ICurrentTenantContext _tenant;

    public LinkPageToBoardItemCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IWorkspacePermissionService permissions,
        IDateTimeProvider timeProvider,
        IResourceReferenceResolver resourceResolver,
        ICurrentTenantContext tenant)
    {
        _context = context;
        _currentUser = currentUser;
        _permissions = permissions;
        _timeProvider = timeProvider;
        _resourceResolver = resourceResolver;
        _tenant = tenant;
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

        await _permissions.EnsureCanEditBoardAsync(card.BoardId, _currentUser.UserId, cancellationToken);

        if (card.WorkspaceId != pageWorkspaceId.Value)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleViolationException("CardPageWorkspaceMismatch", "BoardItem chỉ được link với page cùng workspace.");

        var now = _timeProvider.UtcNow;

        var link = BoardItemLink.Create(
            _tenant.RequireAccountId(),
            card.WorkspaceId,
            card.BoardId,
            card.Id,
            ResourceRef.Create(ResourceType.Page, request.PageId, card.WorkspaceId),
            BoardItemLinkType.Reference,
            _currentUser.UserId,
            now);

        _context.BoardItemLinks.Add(link);

        return Result.Success();
    }
}
