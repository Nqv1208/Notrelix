using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyBoardItems;

public record GetMyBoardItemsQuery(Guid WorkspaceId) : IQuery<Result<List<BoardItemSummaryDto>>>;

public class GetMyBoardItemsQueryHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext) : IRequestHandler<GetMyBoardItemsQuery, Result<List<BoardItemSummaryDto>>>
{
    public async Task<Result<List<BoardItemSummaryDto>>> Handle(GetMyBoardItemsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;

        var memberItemIds = await context.BoardItemMembers
            .Where(m => m.UserId == userId && m.WorkspaceId == request.WorkspaceId)
            .Select(m => m.ItemId)
            .ToListAsync(cancellationToken);

        var items = await context.BoardItems
            .Where(i => memberItemIds.Contains(i.Id) && !i.DeletedAt.HasValue)
            .ToListAsync(cancellationToken);

        var allMembers = await context.BoardItemMembers
            .Where(m => memberItemIds.Contains(m.ItemId))
            .ToListAsync(cancellationToken);

        var allLabels = await context.BoardItemLabels
            .Where(l => memberItemIds.Contains(l.ItemId))
            .ToListAsync(cancellationToken);

        var itemChecklists = await context.Checklists
            .Where(c => memberItemIds.Contains(c.ItemId) && !c.DeletedAt.HasValue)
            .ToListAsync(cancellationToken);

        var checklistIds = itemChecklists.Select(c => c.Id).ToList();
        var checklistItemCounts = await context.ChecklistItems
            .Where(ci => checklistIds.Contains(ci.ChecklistId))
            .GroupBy(ci => ci.ChecklistId)
            .Select(g => new { ChecklistId = g.Key, Total = g.Count(), Done = g.Count(ci => ci.Status == ChecklistItemStatus.Done) })
            .ToListAsync(cancellationToken);

        var result = items.Select(item =>
        {
            var members = allMembers
                .Where(m => m.ItemId == item.Id)
                .Select(m => new BoardItemMemberDto(m.UserId, string.Empty, null, m.AssignedAt))
                .ToList();

            var labels = allLabels
                .Where(l => l.ItemId == item.Id)
                .Select(l => new BoardItemLabelDto(l.LabelId, null, string.Empty))
                .ToList();

            var itemCl = itemChecklists.Where(c => c.ItemId == item.Id).ToList();
            var total = checklistItemCounts.Where(ci => itemCl.Select(c => c.Id).Contains(ci.ChecklistId)).Sum(ci => ci.Total);
            var done = checklistItemCounts.Where(ci => itemCl.Select(c => c.Id).Contains(ci.ChecklistId)).Sum(ci => ci.Done);

            return new BoardItemSummaryDto(
                item.Id,
                item.Name,
                members.Count,
                members,
                labels,
                done,
                total,
                0,
                0,
                item.Position.Value,
                item.CreatedAt.UtcDateTime,
                item.UpdatedAt?.UtcDateTime);
        }).ToList();

        return Result<List<BoardItemSummaryDto>>.Success(result);
    }
}
