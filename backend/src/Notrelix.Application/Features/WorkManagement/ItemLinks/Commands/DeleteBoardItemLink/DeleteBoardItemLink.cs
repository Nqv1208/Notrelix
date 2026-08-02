using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.DeleteBoardItemLink;

[IdempotencyOperation("work-management.item-links.delete-board-item-link.v1")]
public record DeleteBoardItemLinkCommand(Guid BoardItemLinkId, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardItem, BoardItemLinkId);
    public PermissionAction Action => PermissionAction.UpdateItem;
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"delete-item-link:{BoardItemLinkId}";
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
