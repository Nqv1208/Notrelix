using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

public record CreateBoardItemLinkCommand(
    Guid SourceBoardItemId,
    Guid TargetBoardItemId,
    string LinkType,
    string? IdempotencyKey = null) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, SourceBoardItemId);
    public PermissionAction Action => PermissionAction.UpdateItem;
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"create-item-link:{SourceBoardItemId}:{TargetBoardItemId}";
}

public class CreateBoardItemLinkCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider timeProvider) : IRequestHandler<CreateBoardItemLinkCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        var sourceItem = await context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.SourceBoardItemId && !i.DeletedAt.HasValue, cancellationToken);

        if (sourceItem is null)
            throw new NotFoundException(nameof(BoardItem), request.SourceBoardItemId);

        var targetItem = await context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == request.TargetBoardItemId && !i.DeletedAt.HasValue, cancellationToken);

        if (targetItem is null)
            throw new NotFoundException(nameof(BoardItem), request.TargetBoardItemId);

        if (sourceItem.WorkspaceId != targetItem.WorkspaceId)
            throw new BusinessRuleException("Source and target items must belong to the same workspace.");

        if (sourceItem.BoardId != targetItem.BoardId)
            throw new BusinessRuleException("Source and target items must belong to the same board.");

        if (!Enum.TryParse<BoardItemLinkType>(request.LinkType, ignoreCase: true, out var linkType))
            throw new BusinessRuleException($"Invalid link type '{request.LinkType}'.");

        var now = timeProvider.UtcNow;
        var accountId = requestContext.RequireAccountId();
        var workspaceId = requestContext.RequireWorkspaceId();

        var target = ResourceRef.Create(ResourceType.BoardItem, request.TargetBoardItemId, workspaceId);

        var link = BoardItemLink.Create(
            accountId,
            workspaceId,
            sourceItem.BoardId,
            request.SourceBoardItemId,
            target,
            linkType,
            requestContext.UserId,
            now);

        context.BoardItemLinks.Add(link);

        return Result<Guid>.Success(link.Id);
    }
}
