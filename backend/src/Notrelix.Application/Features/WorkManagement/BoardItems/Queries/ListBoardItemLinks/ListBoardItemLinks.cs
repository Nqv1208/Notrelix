using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.ItemLinks;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemLinks;

/// <summary>The other end of a link: the item an item waits for, blocks or relates to.</summary>
public record LinkedItemDto(Guid Id, string? Key, string Title, string? StatusName, string? StatusCategory);

/// <summary>One link as the viewed item sees it: <c>blocked-by</c>, <c>blocks</c> or <c>related</c>.</summary>
public record BoardItemLinkDto(Guid Id, string Relation, LinkedItemDto Item);

public record BoardItemLinkListDto(IReadOnlyList<BoardItemLinkDto> Links);

public record ListBoardItemLinksQuery(Guid BoardItemId) : IQuery<Result<BoardItemLinkListDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class ListBoardItemLinksQueryHandler : IRequestHandler<ListBoardItemLinksQuery, Result<BoardItemLinkListDto>>
{
    private readonly IWorkManagementDbContext _context;

    public ListBoardItemLinksQueryHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<BoardItemLinkListDto>> Handle(ListBoardItemLinksQuery request, CancellationToken ct)
    {
        var itemId = request.BoardItemId;
        var itemKind = ResourceKind.Create("work-management.board-item");

        var rows = (await _context.BoardItemLinks
            .AsNoTracking()
            .Where(l => l.SourceItemId == itemId || l.Target.ResourceId == itemId)
            .ToListAsync(ct))
            .Where(l => l.Target.Kind == itemKind && l.LinkType != BoardItemLinkType.Subtask)
            .ToList();
        if (rows.Count == 0)
            return Result<BoardItemLinkListDto>.Success(new BoardItemLinkListDto([]));

        // The item on the other end of each row, and the relation as the viewed item sees it.
        var ends = rows
            .Select(l => (Link: l, OtherId: l.SourceItemId == itemId ? l.Target.ResourceId : l.SourceItemId))
            .ToList();
        var otherIds = ends.Select(e => e.OtherId).Distinct().ToList();
        var others = await _context.BoardItems.AsNoTracking()
            .Where(i => otherIds.Contains(i.Id) && i.DeletedAt == null)
            .ToListAsync(ct);
        if (others.Count == 0)
            return Result<BoardItemLinkListDto>.Success(new BoardItemLinkListDto([]));

        var boardIds = others.Select(i => i.BoardId).Distinct().ToList();
        var fields = await _context.BoardFields.AsNoTracking()
            .Include(field => field.Options)
            .Where(field => boardIds.Contains(field.BoardId))
            .ToListAsync(ct);

        var statusByItem = new Dictionary<Guid, (string Name, string? Category)>();
        foreach (var board in others.GroupBy(i => i.BoardId))
        {
            var boardFields = fields.Where(f => f.BoardId == board.Key).ToList();
            var status = WorkItemSemanticFields.ResolveStatusField(boardFields);
            if (status is null) continue;
            var values = await BoardItemReadValues.LoadAsync(_context, boardFields, board.Select(i => i.Id).ToList(), ct);
            foreach (var item in board)
            {
                if (values.TryGetValue(item.Id, out var read)
                    && read.StatusOptionId is { } optionId
                    && status.Options.FirstOrDefault(o => o.Id == optionId) is { } option)
                {
                    statusByItem[item.Id] = (option.Name, option.Category is { } category ? WorkStatusCategoryNames.ToName(category) : null);
                }
            }
        }

        var othersById = others.ToDictionary(i => i.Id);
        var links = ends
            .Where(e => othersById.ContainsKey(e.OtherId))
            .Select(e =>
            {
                var other = othersById[e.OtherId];
                statusByItem.TryGetValue(other.Id, out var status);
                return new BoardItemLinkDto(
                    e.Link.Id,
                    ItemLinkRelations.ToRelation(e.Link.LinkType, e.Link.SourceItemId == itemId),
                    new LinkedItemDto(other.Id, other.ItemKey, other.Name, status.Name, status.Category));
            })
            .ToList();

        return Result<BoardItemLinkListDto>.Success(new BoardItemLinkListDto(links));
    }
}
