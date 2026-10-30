using System.Text.Json;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.WorkManagement.Formulas;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;

public record GetFullBoardQuery(Guid BoardId) : IQuery<Result<FullBoardDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetFullBoardQueryHandler : IRequestHandler<GetFullBoardQuery, Result<FullBoardDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly IWorkManagementCollaborationReadPort _collabReadPort;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetFullBoardQueryHandler(
        IWorkManagementDbContext context,
        IActorLookupService actorLookup,
        IWorkManagementCollaborationReadPort collabReadPort,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _actorLookup = actorLookup;
        _collabReadPort = collabReadPort;
        _visibility = visibility;
        _requestContext = requestContext;
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
        // Group items are the non-archived root Items; child Items appear only in
        // their parent's progress and in the item detail.
        var cards = await _context.BoardItems
            .AsNoTracking()
            .Where(card => listIds.Contains(card.GroupId) && card.DeletedAt == null && !card.IsArchived && card.ParentItemId == null)
            .OrderBy(card => card.Position)
            .ToListAsync(cancellationToken);

        var cardIds = cards.Select(card => card.Id).ToList();

        var cardMemberEntities = await _context.BoardItemMembers
            .AsNoTracking()
            .Where(member => cardIds.Contains(member.ItemId))
            .ToListAsync(cancellationToken);

        var memberUserIds = cardMemberEntities.Select(m => m.UserId).Distinct().ToList();
        var memberActors = await _actorLookup.FindManyAsync(memberUserIds, cancellationToken);
        var memberActorMap = memberActors.ToDictionary(a => a.UserId);

        var cardMembers = cardMemberEntities
            .Select(member => new
            {
                member.ItemId,
                Dto = new BoardItemMemberDto(
                    member.UserId,
                    memberActorMap.TryGetValue(member.UserId, out var actor) ? actor.Name : "Unknown",
                    memberActorMap.TryGetValue(member.UserId, out var a) ? a.AvatarUrl : null,
                    member.AssignedAt)
            })
            .ToList();

        var cardLabels = await _context.BoardItemLabels
            .AsNoTracking()
            .Where(cardLabel => cardIds.Contains(cardLabel.ItemId))
            .Join(_context.Labels.AsNoTracking(),
                cardLabel => cardLabel.LabelId,
                label => label.Id,
                (cardLabel, label) => new
                {
                    cardLabel.ItemId,
                    Dto = new BoardItemLabelDto(label.Id, label.Name, label.Color.Hex)
                })
            .ToListAsync(cancellationToken);

        var checklistItems = await _context.ChecklistItems
            .AsNoTracking()
            .Join(_context.Checklists.AsNoTracking().Where(checklist => cardIds.Contains(checklist.ItemId)),
                item => item.ChecklistId,
                checklist => checklist.Id,
                (item, checklist) => new { checklist.ItemId, IsDone = item.Status == ChecklistItemStatus.Done })
            .ToListAsync(cancellationToken);

        var dependencyKind = ResourceKind.Create("work-management.board-item");
        var blockerLinks = (await _context.BoardItemLinks
                .AsNoTracking()
                .Where(link => link.LinkType == BoardItemLinkType.Dependency && cardIds.Contains(link.SourceItemId))
                .ToListAsync(cancellationToken))
            .Where(link => link.Target.Kind == dependencyKind)
            .ToList();
        var blockerIds = blockerLinks.Select(link => link.Target.ResourceId).Distinct().ToList();
        var openBlockerIds = (await _context.BoardItems
                .AsNoTracking()
                .Where(item => blockerIds.Contains(item.Id) && item.DeletedAt == null && item.CompletedAt == null)
                .Select(item => item.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var openBlockerCountByCardId = blockerLinks
            .Where(link => openBlockerIds.Contains(link.Target.ResourceId))
            .GroupBy(link => link.SourceItemId)
            .ToDictionary(group => group.Key, group => group.Count());

        var collaborationCounts = await _collabReadPort.GetCountsAsync(cardIds, cancellationToken);

        var fields = await _context.BoardFields
            .AsNoTracking()
            .Include(field => field.Options)
            .Where(field => field.BoardId == request.BoardId)
            .ToListAsync(cancellationToken);
        var emptyValue = JsonValue.Null();
        // One grouped count for the whole Board: live Items holding a non-empty value, by field.
        var valueCounts = await _context.BoardItemValues
            .AsNoTracking()
            .Where(value => value.Value.Data != emptyValue)
            .Join(
                _context.BoardItems.AsNoTracking().Where(item => item.BoardId == request.BoardId && item.DeletedAt == null && !item.IsArchived),
                value => value.ItemId,
                item => item.Id,
                (value, item) => value.FieldId)
            .GroupBy(fieldId => fieldId)
            .Select(group => new { FieldId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.FieldId, row => row.Count, cancellationToken);
        // Live Items per option of Select/Status fields. shortcut: counted in memory from one projected read; push into SQL if boards grow past tens of thousands of Items.
        var optionFieldIds = fields.Where(f => f.Type is FieldType.Select or FieldType.Status).Select(f => f.Id).ToList();
        var optionValues = await _context.BoardItemValues
            .AsNoTracking()
            .Where(value => optionFieldIds.Contains(value.FieldId))
            .Join(
                _context.BoardItems.AsNoTracking().Where(item => item.BoardId == request.BoardId && item.DeletedAt == null && !item.IsArchived),
                value => value.ItemId,
                item => item.Id,
                (value, item) => new { value.FieldId, value.Value })
            .ToListAsync(cancellationToken);
        var optionCounts = optionValues
            .Select(row => WorkItemSemanticFields.ReadOptionId(row.Value))
            .OfType<Guid>()
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());
        var readValues = await BoardItemReadValues.LoadAsync(_context, fields, cardIds, cancellationToken);
        var fieldProjection = await BoardItemFieldProjection.LoadAsync(_context, fields, cardIds, cancellationToken, _requestContext.UserId);

        var membersByCardId = cardMembers
            .GroupBy(member => member.ItemId)
            .ToDictionary(group => group.Key, group => group.Select(member => member.Dto).ToList());
        var labelsByCardId = cardLabels
            .GroupBy(label => label.ItemId)
            .ToDictionary(group => group.Key, group => group.Select(label => label.Dto).ToList());
        var checklistStatsByCardId = checklistItems
            .GroupBy(item => item.ItemId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Done = group.Count(item => item.IsDone),
                    Total = group.Count()
                });

        var cardsByListId = cards
            .GroupBy(card => card.GroupId)
            .ToDictionary(group => group.Key, group => group
                .OrderBy(card => card.Position)
                .Select(card =>
                {
                    checklistStatsByCardId.TryGetValue(card.Id, out var checklistStats);
                    collaborationCounts.TryGetValue(card.Id, out var collaborationStats);
                    var values = readValues[card.Id];
                    var projected = fieldProjection[card.Id];

                    return new BoardItemSummaryDto(
                        card.Id,
                        card.Name,
                        membersByCardId.TryGetValue(card.Id, out var cardMemberDtos) ? cardMemberDtos.Count : 0,
                        membersByCardId.TryGetValue(card.Id, out var memberDtos) ? memberDtos : [],
                        labelsByCardId.TryGetValue(card.Id, out var labelDtos) ? labelDtos : [],
                        checklistStats?.Done ?? 0,
                        checklistStats?.Total ?? 0,
                        collaborationStats?.CommentCount ?? 0,
                        collaborationStats?.AttachmentCount ?? 0,
                        card.Position.Value,
                        card.CreatedAt.UtcDateTime,
                        card.UpdatedAt?.UtcDateTime,
                        card.Version,
                        card.ItemKey,
                        card.ParentItemId,
                        values.StatusOptionId,
                        values.PriorityOptionId,
                        card.DueAt,
                        card.StartedAt,
                        card.CompletedAt,
                        values.ChildProgress,
                        projected.Stored.ToDictionary(pair => pair.Key, pair => pair.Value),
                        projected.Computed.ToDictionary(pair => pair.Key, pair => pair.Value),
                        openBlockerCountByCardId.GetValueOrDefault(card.Id),
                        card.CoverColor,
                        card.CoverAttachmentId,
                        card.CoverStyle,
                        card.RepeatRule,
                        card.ReminderOffsetMinutes ?? ItemReminder.DefaultMinutes
                    );
                })
                .ToList());

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
            board.IsArchived
        ));
    }

    private static List<BoardFieldOptionDto>? OptionsOf(BoardField field, IReadOnlyDictionary<Guid, int> optionCounts) =>
        field.Type is FieldType.Select or FieldType.Status
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
