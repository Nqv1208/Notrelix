using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.ItemLinks.Commands.CreateBoardItemLink;

[IdempotencyOperation("work-management.item-links.create-board-item-link.v1")]
/// <summary>
/// Says "<see cref="BoardItemId"/> <see cref="Relation"/> <see cref="OtherBoardItemId"/>", with the relation one of
/// <c>blocked-by</c>, <c>blocks</c> or <c>related</c>; the stored row is directed accordingly.
/// </summary>
public record CreateBoardItemLinkCommand(
    Guid BoardItemId,
    Guid OtherBoardItemId,
    string Relation) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
    public PermissionAction Action => PermissionAction.UpdateItem;
}

public class CreateBoardItemLinkCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider timeProvider,
    IWorkOrderingScopeLock scopeLock,
    IBoardRealtimeNotifier? realtime = null) : IRequestHandler<CreateBoardItemLinkCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        var response = await HandleCore(request, cancellationToken);
        if (realtime is not null) await realtime.NotifyAsync(request, response, cancellationToken);
        return response;
    }

    private async Task<Result<Guid>> HandleCore(CreateBoardItemLinkCommand request, CancellationToken cancellationToken)
    {
        if (request.BoardItemId == request.OtherBoardItemId)
            throw new BusinessRuleException("An item cannot be linked to itself.");

        if (!ItemLinkRelations.TryToRow(request.Relation, request.BoardItemId, request.OtherBoardItemId, out var sourceId, out var targetId, out var linkType))
            throw new BusinessRuleException($"Invalid link relation '{request.Relation}'.");

        var sourceItem = await context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == sourceId && !i.DeletedAt.HasValue, cancellationToken);

        if (sourceItem is null)
            throw new NotFoundException(nameof(BoardItem), sourceId);

        var targetItem = await context.BoardItems
            .FirstOrDefaultAsync(i => i.Id == targetId && !i.DeletedAt.HasValue, cancellationToken);

        if (targetItem is null)
            throw new NotFoundException(nameof(BoardItem), targetId);

        if (sourceItem.WorkspaceId != targetItem.WorkspaceId)
            throw new BusinessRuleException("Source and target items must belong to the same workspace.");

        if (sourceItem.BoardId != targetItem.BoardId)
            throw new BusinessRuleException("Source and target items must belong to the same board.");

        // The same pair is linked once: a repeat of the same relation returns the link that exists.
        var existing = (await context.BoardItemLinks
            .Where(l => l.SourceItemId == sourceId && l.Target.ResourceId == targetId)
            .ToListAsync(cancellationToken))
            .FirstOrDefault(l => l.LinkType == linkType);
        if (existing is not null)
            return Result<Guid>.Success(existing.Id);

        if (linkType == BoardItemLinkType.Dependency)
        {
            // Serialize waits-on writes per board so two opposite links cannot both pass the check.
            await scopeLock.AcquireAsync(new WorkOrderingScope(WorkOrderingScopeKind.ItemLinks, sourceItem.BoardId), cancellationToken);

            var edges = (await context.BoardItemLinks
                .Where(l => l.BoardId == sourceItem.BoardId)
                .ToListAsync(cancellationToken))
                .Where(l => l.LinkType == BoardItemLinkType.Dependency)
                .GroupBy(l => l.SourceItemId)
                .ToDictionary(g => g.Key, g => new ItemDependencySnapshot(g.Key, g.Select(l => l.Target.ResourceId).ToList()));

            var itemIds = edges.Keys.Concat(edges.Values.SelectMany(e => e.DependencyIds)).Append(sourceId).Append(targetId).Distinct().ToList();
            var titles = await context.BoardItems
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Name, cancellationToken);

            DependencyRules.EnsureNoCycle(sourceId, targetId, edges, titles);
        }

        var now = timeProvider.UtcNow;
        var accountId = requestContext.RequireAccountId();
        var workspaceId = requestContext.RequireWorkspaceId();

        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), targetId, workspaceId);

        var link = BoardItemLink.Create(
            accountId,
            workspaceId,
            sourceItem.BoardId,
            sourceId,
            target,
            linkType,
            requestContext.UserId,
            now);

        context.BoardItemLinks.Add(link);

        return Result<Guid>.Success(link.Id);
    }
}
