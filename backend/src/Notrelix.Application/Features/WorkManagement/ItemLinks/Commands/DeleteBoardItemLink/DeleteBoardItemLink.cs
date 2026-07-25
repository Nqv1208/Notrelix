using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.DeleteBoardItemLink;

public record DeleteBoardItemLinkCommand(Guid BoardItemLinkId) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemLinkId);
    public PermissionAction Action => PermissionAction.UpdateItem;
}

public class DeleteBoardItemLinkCommandHandler(
    IWorkManagementDbContext context) : IRequestHandler<DeleteBoardItemLinkCommand, Result>
{
    public async Task<Result> Handle(DeleteBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await context.BoardItemLinks
            .FirstOrDefaultAsync(l => l.Id == request.BoardItemLinkId, cancellationToken);

        if (link is null)
            throw new NotFoundException(nameof(BoardItemLink), request.BoardItemLinkId);

        context.BoardItemLinks.Remove(link);

        return Result.Success();
    }
}
