using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListChildItems;

/// <summary>The sub-items of an item in their order, archived ones left out.</summary>
public record ListChildItemsQuery(Guid ParentItemId)
    : IQuery<ChildItemListDto>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ParentItemId);
}

public class ListChildItemsQueryHandler : IRequestHandler<ListChildItemsQuery, ChildItemListDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actorLookup;

    public ListChildItemsQueryHandler(IWorkManagementDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
    }

    public async Task<ChildItemListDto> Handle(ListChildItemsQuery request, CancellationToken ct)
    {
        var parentExists = await _context.BoardItems.AsNoTracking()
            .AnyAsync(item => item.Id == request.ParentItemId && item.DeletedAt == null, ct);
        if (!parentExists) throw new NotFoundException("BoardItem", request.ParentItemId);

        var children = await _context.BoardItems.AsNoTracking()
            .Where(item => item.ParentItemId == request.ParentItemId && item.DeletedAt == null && !item.IsArchived)
            .OrderBy(item => item.Position)
            .ToListAsync(ct);

        var childIds = children.Select(child => child.Id).ToList();
        var members = await _context.BoardItemMembers.AsNoTracking()
            .Where(member => childIds.Contains(member.ItemId))
            .OrderBy(member => member.AssignedAt)
            .ToListAsync(ct);
        var firstMemberByItem = members
            .GroupBy(member => member.ItemId)
            .ToDictionary(group => group.Key, group => group.First().UserId);
        var actors = (await _actorLookup.FindManyAsync(firstMemberByItem.Values.Distinct().ToList(), ct))
            .ToDictionary(actor => actor.UserId);

        var items = children.Select(child =>
        {
            Guid? assigneeId = firstMemberByItem.TryGetValue(child.Id, out var userId) ? userId : null;
            var assigneeName = assigneeId is { } id && actors.TryGetValue(id, out var actor) ? actor.Name : null;
            return new ChildItemDto(
                child.Id, child.ItemKey, child.Name, child.CompletedAt is not null, child.DueAt,
                assigneeId, assigneeName, child.Position.Value, child.Version);
        }).ToList();

        return new ChildItemListDto(items);
    }
}
