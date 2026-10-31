using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardLaneItems;

/// <summary>An Item of a lane page with the group it sits in (the board read nests Items by group).</summary>
public record BoardLaneItemDto(Guid GroupId, BoardItemSummaryDto Item);

/// <summary>One page of a Status lane's Items under a filter and sort, with the lane's size before and after the filter.</summary>
public record BoardLaneItemPageDto(
    Guid? OptionId,
    List<BoardLaneItemDto> Items,
    // Cursor of the next page; null when this page holds the rest.
    string? NextCursor,
    // Live root Items in the lane before the filter.
    int ItemCount,
    // Items in the lane that pass the filter.
    int FilteredCount);

/// <param name="Lane">A Status option id, or "none" for the lane of Items with no status.</param>
/// <param name="Cursor">The previous page's NextCursor; none reads the first page.</param>
/// <param name="Limit">Page size, 50 when absent, clamped to 1..200.</param>
public record ListBoardLaneItemsQuery(Guid BoardId, string Lane, string? Cursor = null, int? Limit = null, string? Filter = null, string? Sort = null)
    : IQuery<BoardLaneItemPageDto>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListBoardLaneItemsQueryHandler : IRequestHandler<ListBoardLaneItemsQuery, BoardLaneItemPageDto>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardItemSummaryBuilder _summaryBuilder;
    private readonly IBoardItemPageReader _pageReader;

    public ListBoardLaneItemsQueryHandler(
        IWorkManagementDbContext context,
        IBoardItemSummaryBuilder summaryBuilder,
        IBoardItemPageReader pageReader)
    {
        _context = context;
        _summaryBuilder = summaryBuilder;
        _pageReader = pageReader;
    }

    public async Task<BoardLaneItemPageDto> Handle(ListBoardLaneItemsQuery request, CancellationToken cancellationToken)
    {
        var criteria = BoardItemCriteria.Parse(request.Filter, request.Sort);
        var offset = PageCursor.Decode(request.Cursor);
        var limit = PageCursor.ClampLimit(request.Limit);
        Guid? optionId = null;
        if (request.Lane != "none")
        {
            if (!Guid.TryParse(request.Lane, out var parsed)) throw BoardItemCriteria.Refuse("lane", "The lane is not a status option or 'none'.");
            optionId = parsed;
        }

        var groupIds = await _context.BoardGroups
            .AsNoTracking()
            .Where(g => g.BoardId == request.BoardId && g.DeletedAt == null && !g.IsArchived)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);
        var fields = await _context.BoardFields
            .AsNoTracking()
            .Include(field => field.Options)
            .Where(field => field.BoardId == request.BoardId)
            .ToListAsync(cancellationToken);
        var laneField = GetFullBoardQueryHandler.LaneFieldOf(fields);
        if (optionId is not null && laneField?.OptionOrder.Contains(optionId.Value) != true)
        {
            throw new NotFoundException("StatusLane", optionId.Value);
        }

        var page = (await _pageReader.ReadAsync(
            request.BoardId, groupIds, laneField, GetFullBoardQueryHandler.CriteriaFieldsOf(fields), criteria,
            optionId?.ToString() ?? "", offset, limit, cancellationToken))
            .Single();

        var loaded = await _context.BoardItems.AsNoTracking().Where(card => page.ItemIds.Contains(card.Id)).ToListAsync(cancellationToken);
        var byId = loaded.ToDictionary(card => card.Id);
        var cards = page.ItemIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        var summaries = await _summaryBuilder.BuildAsync(fields, cards, cancellationToken);
        var items = summaries.Select(summary => new BoardLaneItemDto(byId[summary.Id].GroupId, summary)).ToList();

        return new BoardLaneItemPageDto(
            optionId, items, PageCursor.Encode(offset + page.ItemIds.Count, page.FilteredTotal), page.Total, page.FilteredTotal);
    }
}
