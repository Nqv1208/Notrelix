using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>Builds the board-item summaries a board read shows for a given set of Items: people, labels, checklist progress, blockers, counts and field values.</summary>
public interface IBoardItemSummaryBuilder
{
    Task<List<BoardItemSummaryDto>> BuildAsync(IReadOnlyList<BoardField> fields, IReadOnlyList<BoardItem> cards, CancellationToken cancellationToken);
}

public sealed class BoardItemSummaryBuilder : IBoardItemSummaryBuilder
{
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly IWorkManagementCollaborationReadPort _collabReadPort;
    private readonly ICurrentRequestContext _requestContext;

    public BoardItemSummaryBuilder(
        IWorkManagementDbContext context,
        IActorLookupService actorLookup,
        IWorkManagementCollaborationReadPort collabReadPort,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _actorLookup = actorLookup;
        _collabReadPort = collabReadPort;
        _requestContext = requestContext;
    }

    public async Task<List<BoardItemSummaryDto>> BuildAsync(IReadOnlyList<BoardField> fields, IReadOnlyList<BoardItem> cards, CancellationToken cancellationToken)
    {
        var cardIds = cards.Select(card => card.Id).ToList();

        var cardMemberEntities = await _context.BoardItemMembers
            .AsNoTracking()
            .Where(member => cardIds.Contains(member.ItemId))
            .ToListAsync(cancellationToken);

        var memberUserIds = cardMemberEntities.Select(m => m.UserId)
            .Concat(cards.Where(card => card.UpdatedBy.HasValue).Select(card => card.UpdatedBy!.Value))
            .Distinct().ToList();
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
                    Dto = new BoardItemLabelDto(label.Id, label.Name, label.Color.Hex, null)
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

        return cards.Select(card =>
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
                card.ReminderOffsetMinutes ?? ItemReminder.DefaultMinutes,
                card.UpdatedBy,
                card.UpdatedBy.HasValue && memberActorMap.TryGetValue(card.UpdatedBy.Value, out var editor) ? editor.Name : null
            );
        })
        .ToList();
    }
}
