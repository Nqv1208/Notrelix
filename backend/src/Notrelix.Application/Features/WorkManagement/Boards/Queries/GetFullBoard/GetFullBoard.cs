using System.Text.Json;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;

public record GetFullBoardQuery(Guid BoardId) : IQuery<Result<FullBoardDto>>;

public class GetFullBoardQueryHandler : IRequestHandler<GetFullBoardQuery, Result<FullBoardDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly ICollaborationDbContext _collabContext;

    public GetFullBoardQueryHandler(IWorkManagementDbContext context, IActorLookupService actorLookup, ICollaborationDbContext collabContext)
    {
        _context = context;
        _actorLookup = actorLookup;
        _collabContext = collabContext;
    }

    public async Task<Result<FullBoardDto>> Handle(GetFullBoardQuery request, CancellationToken cancellationToken)
    {
        var board = await _context.Boards
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && !b.IsArchived, cancellationToken);

        if (board == null)
            throw new NotFoundException(nameof(Board), request.BoardId);

        var lists = await _context.BoardGroups
            .AsNoTracking()
            .Where(l => l.BoardId == request.BoardId && l.DeletedAt == null)
            .OrderBy(l => l.Position)
            .ToListAsync(cancellationToken);

        var listIds = lists.Select(list => list.Id).ToList();
        var cards = await _context.BoardItems
            .AsNoTracking()
            .Where(card => listIds.Contains(card.GroupId) && !card.IsDeleted)
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

        var commentCounts = await _collabContext.Comments
            .AsNoTracking()
            .Where(comment => comment.Target.ResourceType == ResourceType.BoardItem && cardIds.Contains(comment.Target.ResourceId))
            .GroupBy(comment => comment.Target.ResourceId)
            .Select(group => new { BoardItemId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BoardItemId, item => item.Count, cancellationToken);

        var attachmentCounts = await _collabContext.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.Target.ResourceType == ResourceType.BoardItem && cardIds.Contains(attachment.Target.ResourceId))
            .GroupBy(attachment => attachment.Target.ResourceId)
            .Select(group => new { BoardItemId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BoardItemId, item => item.Count, cancellationToken);

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

                    return new BoardItemSummaryDto(
                        card.Id,
                        card.Name,
                        membersByCardId.TryGetValue(card.Id, out var cardMemberDtos) ? cardMemberDtos.Count : 0,
                        membersByCardId.TryGetValue(card.Id, out var memberDtos) ? memberDtos : [],
                        labelsByCardId.TryGetValue(card.Id, out var labelDtos) ? labelDtos : [],
                        checklistStats?.Done ?? 0,
                        checklistStats?.Total ?? 0,
                        commentCounts.GetValueOrDefault(card.Id),
                        attachmentCounts.GetValueOrDefault(card.Id),
                        card.Position.Value,
                        card.CreatedAt.DateTime,
                        card.UpdatedAt?.DateTime
                    );
                })
                .ToList());

        var listDtos = lists
            .Select(list => new BoardGroupDto(
                list.Id,
                list.Title,
                list.Color.ToString(),
                list.Position.Value,
                list.DeletedAt != null,
                cardsByListId.GetValueOrDefault(list.Id) ?? []))
            .ToList();

        var columns = await _context.BoardFields
            .AsNoTracking()
            .Where(column => column.BoardId == request.BoardId)
            .OrderBy(column => column.Position)
            .Select(column => new BoardFieldDto(
                column.Id,
                column.BoardId,
                column.Name,
                column.Type.ToString(),
                JsonSerializer.Serialize(column.Settings, (JsonSerializerOptions?)null),
                column.DefaultValue,
                column.Position.Value,
                column.IsSystem,
                column.IsDeleted
            ))
            .ToListAsync(cancellationToken);

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
                false
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
                m.JoinedAt.DateTime
            ))
            .ToList();

        return Result<FullBoardDto>.Success(new FullBoardDto(
            board.Id,
            board.WorkspaceId,
            board.Title,
            board.Description,
            board.Background,
            board.Visibility.ToString(),
            columns,
            listDtos,
            members
        ));
    }

    private static bool IsTitleColumn(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return normalized is "task" or "title" or "name";
    }
}
