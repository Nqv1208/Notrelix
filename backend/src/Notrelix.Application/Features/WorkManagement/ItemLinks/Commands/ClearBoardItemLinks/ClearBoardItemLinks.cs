using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.ItemLinks;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.ClearBoardItemLinks;

/// <summary>
/// Removes every item link the Item has, at either end (blocked-by, blocks and related), in one change. An Item
/// with no links is left as it is. Parent and sub-item structure and linked documents are not links here.
/// </summary>
[IdempotencyOperation("work-management.item-links.clear-board-item-links.v1")]
public record ClearBoardItemLinksCommand(Guid BoardItemId)
    : ICommand<Result<int>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
    public PermissionAction Action => PermissionAction.UpdateItem;
}

public class ClearBoardItemLinksCommandHandler(IWorkManagementDbContext context,
    IBoardRealtimeNotifier? realtime = null) : IRequestHandler<ClearBoardItemLinksCommand, Result<int>>
{
    public async Task<Result<int>> Handle(ClearBoardItemLinksCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (realtime is not null) await realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result<int>> HandleCore(ClearBoardItemLinksCommand request, CancellationToken ct)
    {
        var itemKind = ResourceKind.Create("work-management.board-item");
        var links = (await context.BoardItemLinks
                .Where(l => l.SourceItemId == request.BoardItemId || l.Target.ResourceId == request.BoardItemId)
                .ToListAsync(ct))
            .Where(l => l.Target.Kind == itemKind && l.LinkType != BoardItemLinkType.Subtask)
            .ToList();

        context.BoardItemLinks.RemoveRange(links);
        return Result<int>.Success(links.Count);
    }
}
