using System.Text.Json;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Domain.WorkManagement.Formulas;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;

/// <param name="PageSize">When set (clamped to 1..200), each group carries only its first page of Items, in the order of <paramref name="Sort"/>, among those that pass <paramref name="Filter"/>; null reads every Item.</param>
public record GetFullBoardQuery(Guid BoardId, int? PageSize = null, string? Filter = null, string? Sort = null) : IQuery<Result<FullBoardDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetFullBoardQueryHandler : IRequestHandler<GetFullBoardQuery, Result<FullBoardDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly IBoardItemSummaryBuilder _summaryBuilder;
    private readonly IBoardItemPageReader _pageReader;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetFullBoardQueryHandler(
        IWorkManagementDbContext context,
        IActorLookupService actorLookup,
        IBoardItemSummaryBuilder summaryBuilder,
        IBoardItemPageReader pageReader,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _actorLookup = actorLookup;
        _summaryBuilder = summaryBuilder;
        _pageReader = pageReader;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    /// <summary>The Status field a paged read lays the board out by: the first live Status field.</summary>
    internal static CriteriaField? LaneFieldOf(IReadOnlyList<BoardField> fields) =>
        CriteriaFieldsOf(fields).FirstOrDefault(f => f.Type == FieldType.Status);

    /// <summary>What the criteria need of the board's fields: type, option order and which Date field stands for the due date.</summary>
    internal static List<CriteriaField> CriteriaFieldsOf(IReadOnlyList<BoardField> fields)
    {
        var live = fields.Where(f => f.DeletedAt == null).OrderBy(f => f.Position).ToList();
        var dueId = live.Where(f => f.Type == FieldType.Date).Select(f => (Guid?)f.Id).FirstOrDefault();
        // The client's own reading of people: the first field that names an owner or assignee (or holds people)
        // mirrors the Item's members; the first Person field falls back to them.
        static bool NamesPerson(BoardField f)
        {
            var name = f.Name.Trim().ToLowerInvariant();
            return name.Contains("owner") || name.Contains("assignee") || f.Type == FieldType.Person;
        }

        var mirrorsPeopleId = live.Where(NamesPerson).Select(f => (Guid?)f.Id).FirstOrDefault();
        var firstPersonId = live.Where(f => f.Type == FieldType.Person).Select(f => (Guid?)f.Id).FirstOrDefault();
        return live
            .Select(f =>
            {
                var name = f.Name.Trim().ToLowerInvariant();
                var person = f.Id == mirrorsPeopleId && f.Type == FieldType.Person ? PersonSource.Members
                    : f.Id == firstPersonId ? PersonSource.StoredOrMembers
                    : PersonSource.Stored;
                return new CriteriaField(
                    f.Id,
                    f.Type,
                    f.Id == dueId,
                    name.Contains("due") && name.Contains("date"),
                    person,
                    f.Options.OrderBy(option => option.Position).ThenBy(option => option.Id).Select(option => option.Id).ToList());
            })
            .ToList();
    }

    public async Task<Result<FullBoardDto>> Handle(GetFullBoardQuery request, CancellationToken cancellationToken)
    {
        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken);

        if (board == null)
            throw new NotFoundException(nameof(Board), request.BoardId);

        var lists = await _context.BoardGroups
            .AsNoTracking()
            .Where(l => l.BoardId == request.BoardId && l.DeletedAt == null)
            .OrderBy(l => l.Position)
            .ToListAsync(cancellationToken);

        // An archived group is listed (the settings offer to unarchive it) but its Items are not: they
        // stay in the group and come back with it.
        var archivedListIds = lists.Where(list => list.IsArchived).Select(list => list.Id).ToList();
        var listIds = lists.Where(list => !list.IsArchived).Select(list => list.Id).ToList();
        var archivedItemCounts = archivedListIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.BoardItems
                .AsNoTracking()
                .Where(card => archivedListIds.Contains(card.GroupId) && card.DeletedAt == null && !card.IsArchived && card.ParentItemId == null)
                .GroupBy(card => card.GroupId)
                .Select(group => new { GroupId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.GroupId, row => row.Count, cancellationToken);
        var fields = await _context.BoardFields
            .AsNoTracking()
            .Include(field => field.Options)
            .Where(field => field.BoardId == request.BoardId)
            .ToListAsync(cancellationToken);
        // Group items are the non-archived root Items; child Items appear only in
        // their parent's progress and in the item detail. A paged read takes the first page of each group, in
        // the criteria's order, and reports each group's size before and after the filter.
        List<BoardItem> cards;
        List<BoardLaneDto>? lanes = null;
        if (request.PageSize is { } pageSize)
        {
            var criteria = BoardItemCriteria.Parse(request.Filter, request.Sort);
            var laneField = LaneFieldOf(fields);
            var read = await _pageReader.ReadAsync(
                board.Id, listIds, laneField, CriteriaFieldsOf(fields), criteria, null, 0, PageCursor.ClampLimit(pageSize), cancellationToken);
            lanes = read
                .Select(page => new BoardLaneDto(
                    page.LaneKey == "" ? null : Guid.Parse(page.LaneKey),
                    page.Total,
                    page.FilteredTotal,
                    PageCursor.Encode(page.ItemIds.Count, page.FilteredTotal)))
                .ToList();
            var pageIds = read.SelectMany(page => page.ItemIds).ToList();
            var loaded = await _context.BoardItems.AsNoTracking().Where(card => pageIds.Contains(card.Id)).ToListAsync(cancellationToken);
            var byId = loaded.ToDictionary(card => card.Id);
            cards = pageIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        }
        else
        {
            cards = await _context.BoardItems
                .AsNoTracking()
                .Where(card => listIds.Contains(card.GroupId) && card.DeletedAt == null && !card.IsArchived && card.ParentItemId == null)
                .OrderBy(card => card.Position)
                .ToListAsync(cancellationToken);
        }

        var emptyValue = JsonValue.Null();
        // One grouped count for the whole Board: live Items holding a non-empty value, by field.
        // The board's own fields bound the scan to its values (ix_board_item_values_field_id); without it the
        // join read every value of every board, and the row-level security check ran for each of them.
        var boardFieldIds = fields.Select(f => f.Id).ToList();
        // A paged read leaves these board-wide counts out (each scans every value under row-level security, which
        // is what paging avoids); the settings read the whole board and still get them.
        Dictionary<Guid, int> valueCounts = [];
        Dictionary<Guid, int> optionCounts = [];
        // The lane pass already counted the Status options.
        foreach (var lane in lanes ?? []) if (lane.OptionId is { } laneOption) optionCounts[laneOption] = lane.ItemCount;
        if (request.PageSize is null)
        {
            valueCounts = await _context.BoardItemValues
                .AsNoTracking()
                .Where(value => boardFieldIds.Contains(value.FieldId) && value.Value.Data != emptyValue)
                .Join(
                    _context.BoardItems.AsNoTracking().Where(item => item.BoardId == request.BoardId && item.DeletedAt == null && !item.IsArchived),
                    value => value.ItemId,
                    item => item.Id,
                    (value, item) => value.FieldId)
                .GroupBy(fieldId => fieldId)
                .Select(group => new { FieldId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.FieldId, row => row.Count, cancellationToken);
            // Live Items per option of Select/Status fields. shortcut: counted in memory from one projected read; push into SQL if boards grow past tens of thousands of Items.
            var optionFieldIds = fields.Where(f => f.Type is FieldType.Select or FieldType.Status or FieldType.MultiSelect).Select(f => f.Id).ToList();
            var multiFieldIds = fields.Where(f => f.Type is FieldType.MultiSelect).Select(f => f.Id).ToHashSet();
            var optionValues = await _context.BoardItemValues
                .AsNoTracking()
                .Where(value => optionFieldIds.Contains(value.FieldId))
                .Join(
                    _context.BoardItems.AsNoTracking().Where(item => item.BoardId == request.BoardId && item.DeletedAt == null && !item.IsArchived),
                    value => value.ItemId,
                    item => item.Id,
                    (value, item) => new { value.FieldId, value.Value })
                .ToListAsync(cancellationToken);
            optionCounts = optionValues
                .SelectMany(row => multiFieldIds.Contains(row.FieldId)
                    ? WorkItemSemanticFields.ReadOptionIds(row.Value)
                    : WorkItemSemanticFields.ReadOptionId(row.Value) is { } single ? [single] : Array.Empty<Guid>())
                .GroupBy(id => id)
                .ToDictionary(group => group.Key, group => group.Count());
        }

        var summaries = await _summaryBuilder.BuildAsync(fields, cards, cancellationToken);
        var groupOfCard = cards.ToDictionary(card => card.Id, card => card.GroupId);
        var cardsByListId = summaries
            .GroupBy(summary => groupOfCard[summary.Id])
            .ToDictionary(group => group.Key, group => group.ToList());

        var listDtos = lists
            .Select(list => new BoardGroupDto(
                list.Id,
                list.Title,
                list.Color.ToString(),
                list.Position.Value,
                list.IsArchived,
                cardsByListId.GetValueOrDefault(list.Id) ?? [],
                list.Version,
                list.IsArchived
                    ? archivedItemCounts.GetValueOrDefault(list.Id)
                    : (cardsByListId.GetValueOrDefault(list.Id) ?? []).Count))
            .ToList();

        var fieldInputs = fields.Where(f => f.DeletedAt == null)
            .Select(f => new ComputedFieldInput(f.Id, f.Name, f.Type, f.Settings.Data.Value)).ToList();
        var columns = fields
            .OrderBy(column => column.Position)
            .Select(column => new BoardFieldDto(
                column.Id,
                column.BoardId,
                column.Name,
                column.Type.ToString(),
                // A Formula's saved field ids are shown as the fields' current names.
                column.Type == FieldType.Formula
                    ? JsonSerializer.Serialize(
                        FieldSettings.Create(JsonValue.Create(ComputedFieldGraph.RenderNames(column.Settings.Data.Value, fieldInputs))), (JsonSerializerOptions?)null)
                    : JsonSerializer.Serialize(column.Settings, (JsonSerializerOptions?)null),
                column.DefaultValue == null ? null : column.DefaultValue.Data.Value,
                column.Position.Value,
                column.IsSystem,
                column.DeletedAt != null,
                column.Version,
                OptionsOf(column, optionCounts),
                valueCounts.GetValueOrDefault(column.Id)
            ))
            .ToList();

        if (!columns.Any(column => IsTitleColumn(column.Name)))
        {
            columns.Insert(0, new BoardFieldDto(
                board.Id,
                board.Id,
                "Task",
                "text",
                """{"system":"title"}""",
                null,
                "a0",
                true,
                false,
                // Synthetic, read-only title column; it has no aggregate version.
                0
            ));
        }

        var boardMemberEntities = await _context.BoardMembers
            .AsNoTracking()
            .Where(m => m.BoardId == request.BoardId)
            .ToListAsync(cancellationToken);

        var boardMemberUserIds = boardMemberEntities.Select(m => m.UserId).Distinct().ToList();
        var boardMemberActors = await _actorLookup.FindManyAsync(boardMemberUserIds, cancellationToken);
        var boardMemberActorMap = boardMemberActors.ToDictionary(a => a.UserId);

        var members = boardMemberEntities
            .Select(m => new BoardMemberDto(
                m.UserId,
                boardMemberActorMap.TryGetValue(m.UserId, out var actor) ? actor.Name : "Unknown",
                boardMemberActorMap.TryGetValue(m.UserId, out var a) ? a.AvatarUrl : null,
                m.Role.ToString(),
                m.JoinedAt.UtcDateTime
            ))
            .ToList();

        // A board reachable through an explicit grant may sit in a Space the
        // caller cannot see; that Space's id is not disclosed.
        Guid? spaceId = null;
        if (board.SpaceId is { } placedIn
            && (await _visibility.VisibleSpaceIdsAsync(board.WorkspaceId, _requestContext.UserId, cancellationToken)).Contains(placedIn))
        {
            spaceId = placedIn;
        }

        return Result<FullBoardDto>.Success(new FullBoardDto(
            board.Id,
            board.WorkspaceId,
            board.Title,
            board.Description,
            board.Background,
            board.Visibility.ToString(),
            columns,
            listDtos,
            members,
            board.Version,
            spaceId,
            string.IsNullOrWhiteSpace(board.ItemKeyPrefix) ? null : board.ItemKeyPrefix,
            board.IsArchived,
            lanes?.Sum(lane => lane.FilteredCount),
            lanes?.Sum(lane => lane.ItemCount),
            lanes
        ));
    }

    private static List<BoardFieldOptionDto>? OptionsOf(BoardField field, IReadOnlyDictionary<Guid, int> optionCounts) =>
        field.Type is FieldType.Select or FieldType.Status or FieldType.MultiSelect
            ? field.Options
                .OrderBy(option => option.Position)
                .ThenBy(option => option.Id)
                .Select(option => new BoardFieldOptionDto(
                    option.Id,
                    option.Name,
                    option.Color.Value,
                    option.Position.Value,
                    WorkStatusCategoryNames.ToName(option.Category),
                    optionCounts.GetValueOrDefault(option.Id)))
                .ToList()
            : null;

    private static bool IsTitleColumn(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return normalized is "task" or "title" or "name";
    }
}
