using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.DeleteBoardItemLink;

[IdempotencyOperation("work-management.item-links.delete-board-item-link.v1")]
/// <summary>Removes a link seen from <see cref="BoardItemId"/>, at either of its ends.</summary>
public record DeleteBoardItemLinkCommand(Guid BoardItemId, Guid BoardItemLinkId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
    public PermissionAction Action => PermissionAction.UpdateItem;
}

public class DeleteBoardItemLinkCommandHandler(
    IWorkManagementDbContext context) : IRequestHandler<DeleteBoardItemLinkCommand, Result>
{
    public async Task<Result> Handle(DeleteBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await context.BoardItemLinks
            .FirstOrDefaultAsync(l => l.Id == request.BoardItemLinkId
                && (l.SourceItemId == request.BoardItemId || l.Target.ResourceId == request.BoardItemId), cancellationToken);

        if (link is null)
            throw new NotFoundException(nameof(BoardItemLink), request.BoardItemLinkId);

        context.BoardItemLinks.Remove(link);

        return Result.Success();
    }
}
