using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Queries.ListBoardChecklists;

public sealed record BoardChecklistEntryDto(Guid Id, string Title, Guid ItemId, string? ItemKey, string ItemName, int ItemCount);

/// <summary>
/// The checklists of the live items of a board, newest first and bounded, so a member can pick one to copy.
/// Anyone who can view the board sees them, the same as the checklists on each item.
/// </summary>
public record ListBoardChecklistsQuery(Guid BoardId) : IQuery<Result<List<BoardChecklistEntryDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public const int Limit = 200;

    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListBoardChecklistsQueryHandler(IWorkManagementDbContext context) : IRequestHandler<ListBoardChecklistsQuery, Result<List<BoardChecklistEntryDto>>>
{
    public async Task<Result<List<BoardChecklistEntryDto>>> Handle(ListBoardChecklistsQuery request, CancellationToken ct)
    {
        var rows = await (
            from checklist in context.Checklists.AsNoTracking()
            join item in context.BoardItems.AsNoTracking() on checklist.ItemId equals item.Id
            where item.BoardId == request.BoardId && item.DeletedAt == null && !item.IsArchived
            orderby checklist.CreatedAt descending, checklist.Id
            select new { checklist.Id, checklist.Title, ItemId = item.Id, item.ItemKey, ItemName = item.Name, Count = checklist.Items.Count })
            .Take(ListBoardChecklistsQuery.Limit)
            .ToListAsync(ct);

        return Result<List<BoardChecklistEntryDto>>.Success(rows
            .Select(r => new BoardChecklistEntryDto(r.Id, r.Title, r.ItemId, r.ItemKey, r.ItemName, r.Count))
            .ToList());
    }
}
