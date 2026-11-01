using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;

public record GetBoardItemQuery(Guid BoardItemId) : IQuery<Result<BoardItemDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class GetBoardItemQueryHandler : IRequestHandler<GetBoardItemQuery, Result<BoardItemDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actorLookup;
    private readonly IWorkManagementCollaborationReadPort _collabReadPort;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IWorkspaceAccessResolver _workspaceAccess;
    public GetBoardItemQueryHandler(IWorkManagementDbContext context, IActorLookupService actorLookup, IWorkManagementCollaborationReadPort collabReadPort, ICurrentRequestContext requestContext, IWorkspaceAccessResolver workspaceAccess)
    {
        _workspaceAccess = workspaceAccess;
        _requestContext = requestContext;
        _context = context;
        _actorLookup = actorLookup;
        _collabReadPort = collabReadPort;
    }

    public async Task<Result<BoardItemDto>> Handle(GetBoardItemQuery request, CancellationToken ct)
    {
        var card = await _context.BoardItems.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.BoardItemId && c.DeletedAt == null, ct);

        if (card is null) throw await MissingAsync(request.BoardItemId, ct);

        // Load member entities, then batch lookup actors
        var memberEntities = await _context.BoardItemMembers.AsNoTracking()
            .Where(m => m.ItemId == card.Id)
            .ToListAsync(ct);

        var memberUserIds = memberEntities.Select(m => m.UserId).Distinct().ToList();
        var lifecycleUserIds = new[] { card.CompletedBy, card.UpdatedBy }.Where(id => id.HasValue).Select(id => id!.Value);
        memberUserIds = memberUserIds.Concat(lifecycleUserIds).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(memberUserIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        string? NameOf(Guid? userId) => userId.HasValue && actorMap.TryGetValue(userId.Value, out var who) ? who.Name : null;
        var members = memberEntities
            .Select(m => new BoardItemMemberDto(
                m.UserId,
                actorMap.TryGetValue(m.UserId, out var actor) ? actor.Name : "Unknown",
                actorMap.TryGetValue(m.UserId, out var a) ? a.AvatarUrl : null,
                m.AssignedAt))
            .ToList();

        var labels = await _context.BoardItemLabels.AsNoTracking()
            .Where(cl => cl.ItemId == card.Id)
            .Join(_context.Labels.AsNoTracking(), cl => cl.LabelId, l => l.Id,
                (cl, l) => new BoardItemLabelDto(l.Id, l.Name, l.Color.Hex, null))
            .ToListAsync(ct);

        var checklists = await _context.Checklists.AsNoTracking()
            .Where(c => c.ItemId == card.Id)
            .OrderBy(c => c.Position)
            .Select(c => new ChecklistDto(
                c.Id, c.Title, c.Position.Value, c.Version,
                _context.ChecklistItems.AsNoTracking()
                    .Where(i => i.ChecklistId == c.Id)
                    .OrderBy(i => i.Position)
                    .Select(i => new ChecklistItemDto(i.Id, i.Title, i.Status.ToString(), i.DueAt.HasValue ? i.DueAt.Value.UtcDateTime : null, i.AssigneeUserId, i.Position.Value))
                    .ToList()
            )).ToListAsync(ct);

        var listContext = await _context.BoardGroups.AsNoTracking()
            .Where(list => list.Id == card.GroupId)
            .Join(_context.Boards.AsNoTracking(),
                list => list.BoardId,
                board => board.Id,
                (list, board) => new { BoardId = board.Id, board.WorkspaceId })
            .FirstOrDefaultAsync(ct);

        if (listContext is null) throw new NotFoundException("List", card.GroupId);

        var collaborationCounts = await _collabReadPort.GetCountsAsync([card.Id], ct);
        var itemCounts = collaborationCounts.GetValueOrDefault(card.Id);

        var fields = await _context.BoardFields.AsNoTracking()
            .Include(field => field.Options)
            .Where(field => field.BoardId == listContext.BoardId)
            .ToListAsync(ct);
        var values = (await BoardItemReadValues.LoadAsync(_context, fields, [card.Id], ct))[card.Id];
        var projected = (await BoardItemFieldProjection.LoadAsync(_context, fields, [card.Id], ct, _requestContext.UserId))[card.Id];

        return Result<BoardItemDto>.Success(new BoardItemDto(
            card.Id, listContext.BoardId, listContext.WorkspaceId, card.GroupId, card.Name,
            members, labels, checklists,
            itemCounts.CommentCount, itemCounts.AttachmentCount,
            card.Position.Value,
            card.CreatedAt.UtcDateTime, card.UpdatedAt?.UtcDateTime,
            card.Version,
            card.ItemKey,
            card.ParentItemId,
            values.StatusOptionId,
            values.PriorityOptionId,
            card.DueAt,
            card.StartedAt,
            card.CompletedAt,
            values.ChildProgress,
            card.IsArchived,
            card.CompletedBy,
            NameOf(card.CompletedBy),
            card.ArchivedAt,
            card.ArchivedBy,
            await ActorNotice.NameAsync(_actorLookup, _workspaceAccess, listContext.WorkspaceId, _requestContext.UserId, card.ArchivedBy, ct),
            card.DescriptionMd,
            projected.Stored.ToDictionary(pair => pair.Key, pair => pair.Value),
            projected.Computed.ToDictionary(pair => pair.Key, pair => pair.Value),
            card.CoverColor,
            card.CoverAttachmentId,
            card.CoverStyle,
            card.RepeatRule,
            card.ReminderOffsetMinutes ?? ItemReminder.DefaultMinutes,
            card.UpdatedBy,
            NameOf(card.UpdatedBy)
        ));
    }

    /// <summary>
    /// The item is not readable. Authorization already proved the caller reads its board, so a deleted item answers
    /// with the deleted code and who deleted it (a Guest reads no name). A caller without access never reaches this
    /// handler, and a missing id stays the plain not-found.
    /// </summary>
    private async Task<NotFoundException> MissingAsync(Guid itemId, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var deleted = await _context.BoardItems.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.Id == itemId && i.AccountId == accountId && i.WorkspaceId == workspaceId && i.DeletedAt != null)
            .Select(i => new { i.DeletedBy })
            .FirstOrDefaultAsync(ct);
        if (deleted is null) return new NotFoundException("BoardItem", itemId);

        var by = await ActorNotice.NameAsync(_actorLookup, _workspaceAccess, workspaceId, _requestContext.UserId, deleted.DeletedBy, ct);
        return new NotFoundException("BoardItem", itemId)
        {
            ErrorCode = DeletedCode,
            Extensions = new Dictionary<string, object?> { ["deletedByName"] = by },
        };
    }

    public const string DeletedCode = "work-management.item.deleted";
}
