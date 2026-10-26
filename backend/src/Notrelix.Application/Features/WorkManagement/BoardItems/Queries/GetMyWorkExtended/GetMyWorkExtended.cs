using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Approvals.Services;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyWorkExtended;

/// <summary>
/// "My work" as the member's page draws it: the open Items assigned to the caller (soonest due first, undated
/// last) and the Items the caller recently completed, each with its key, Status (label and work-status
/// category), Priority option and the person in charge when it is someone else. Both lists are bounded and come only from Boards the
/// caller may read (the single-read ViewBoard decision, GOV-029). The open Items the caller follows are listed
/// too, with the "following" relation, and so are the open Items the caller is asked to approve (a pending
/// approval request names them), with the "approver" relation and the request. "Waiting on" lists the open Items the caller created or follows,
/// where the caller is not assigned and someone else is, soonest due first (at most <see cref="GetMyWorkExtendedQuery.WaitingLimit"/>),
/// each with the person in charge and when the caller last reminded them.
/// </summary>
public record GetMyWorkExtendedQuery(Guid WorkspaceId)
    : IQuery<Result<MyWorkExtendedDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int OpenLimit = 200;
    public const int DoneLimit = 50;
    public const int WaitingLimit = 20;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public sealed record MyWorkPersonDto(Guid Id, string Name);

/// <summary>A Status option held by an Item: its label and the work-status category it belongs to.</summary>
public sealed record MyWorkStatusDto(Guid OptionId, string Label, string Bucket);

/// <summary>A Priority option held by an Item; the label is the option's own name.</summary>
public sealed record MyWorkPriorityDto(Guid OptionId, string Label);

public sealed record MyWorkApprovalDto(Guid RequestId, MyWorkPersonDto RequestedBy, DateTimeOffset RequestedAt);

public sealed record MyWorkItemExtDto(
    Guid Id,
    Guid BoardId,
    Guid GroupId,
    string? Key,
    string Name,
    DateTimeOffset? DueAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? UpdatedAt,
    long Version,
    MyWorkStatusDto? Status,
    MyWorkPriorityDto? Priority,
    IReadOnlyList<string> Relations,
    MyWorkPersonDto? Assignee,
    MyWorkApprovalDto? Approval,
    DateTimeOffset? CompletedAt);

public sealed record MyWorkWaitingDto(
    Guid ItemId,
    Guid BoardId,
    string? Key,
    string Title,
    MyWorkPersonDto Owner,
    DateTimeOffset? DueAt,
    DateTimeOffset? LastRemindedAt);

public sealed record MyWorkExtendedDto(
    IReadOnlyList<MyWorkItemExtDto> Items,
    IReadOnlyList<MyWorkItemExtDto> Done,
    IReadOnlyList<MyWorkWaitingDto> Waiting,
    IReadOnlyDictionary<string, string?> Departments);

public class GetMyWorkExtendedQueryHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IResourceVisibility visibility,
    IActorLookupService actors,
    IWorkManagementCollaborationReadPort collaboration,
    IBoardItemReminders reminders) : IRequestHandler<GetMyWorkExtendedQuery, Result<MyWorkExtendedDto>>
{
    public async Task<Result<MyWorkExtendedDto>> Handle(GetMyWorkExtendedQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var visibleBoardIds = await visibility.VisibleBoardIdsAsync(request.WorkspaceId, userId, cancellationToken);
        if (visibleBoardIds.Count == 0)
        {
            return Result<MyWorkExtendedDto>.Success(Empty());
        }

        var mine =
            from member in context.BoardItemMembers.AsNoTracking()
            join item in context.BoardItems.AsNoTracking() on member.ItemId equals item.Id
            join board in context.Boards.AsNoTracking() on item.BoardId equals board.Id
            where member.UserId == userId
                  && member.WorkspaceId == request.WorkspaceId
                  && item.WorkspaceId == request.WorkspaceId
                  && board.WorkspaceId == request.WorkspaceId
                  && item.DeletedAt == null
                  && !item.IsArchived
                  && board.DeletedAt == null
                  && !board.IsArchived
                  && visibleBoardIds.Contains(board.Id)
            select item;

        var open = await mine
            .Where(i => i.CompletedAt == null)
            .OrderBy(i => i.DueAt == null)
            .ThenBy(i => i.DueAt)
            .ThenBy(i => i.Id)
            .Take(GetMyWorkExtendedQuery.OpenLimit)
            .ToListAsync(cancellationToken);

        var done = await mine
            .Where(i => i.CompletedAt != null)
            .OrderByDescending(i => i.CompletedAt)
            .ThenBy(i => i.Id)
            .Take(GetMyWorkExtendedQuery.DoneLimit)
            .ToListAsync(cancellationToken);

        // The open Items the caller follows and is not assigned to, from Boards they may still read.
        var followedIds = await collaboration.GetFollowedItemIdsAsync(
            request.WorkspaceId, userId, GetMyWorkExtendedQuery.OpenLimit, cancellationToken);
        var followedOnly = followedIds.Count == 0
            ? []
            : await (
                    from item in context.BoardItems.AsNoTracking()
                    join board in context.Boards.AsNoTracking() on item.BoardId equals board.Id
                    where followedIds.Contains(item.Id)
                          && item.WorkspaceId == request.WorkspaceId
                          && item.DeletedAt == null
                          && !item.IsArchived
                          && item.CompletedAt == null
                          && board.DeletedAt == null
                          && !board.IsArchived
                          && visibleBoardIds.Contains(board.Id)
                    select item)
                .ToListAsync(cancellationToken);
        var followedSet = followedIds.ToHashSet();
        var openIds = open.Select(i => i.Id).ToHashSet();
        followedOnly = followedOnly.Where(i => !openIds.Contains(i.Id)).ToList();

        // The Items the caller is asked to approve; those not already listed are added as approver-only rows.
        var approvals = (await PendingApprovals.ListAsync(
                context, request.WorkspaceId, userId, visibleBoardIds, cancellationToken))
            .GroupBy(a => a.ItemId)
            .ToDictionary(g => g.Key, g => g.First());
        var listedIds = open.Concat(followedOnly).Select(i => i.Id).ToHashSet();
        var approvalOnlyIds = approvals.Keys.Where(id => !listedIds.Contains(id)).ToList();
        var approverOnly = approvalOnlyIds.Count == 0
            ? []
            : await context.BoardItems.AsNoTracking()
                .Where(i => approvalOnlyIds.Contains(i.Id) && i.DeletedAt == null && !i.IsArchived)
                .ToListAsync(cancellationToken);
        followedOnly = followedOnly.Concat(approverOnly).ToList();

        var waiting = await LoadWaitingAsync(request.WorkspaceId, userId, visibleBoardIds, followedSet, cancellationToken);

        var all = open.Concat(followedOnly).Concat(done).ToList();
        if (all.Count == 0)
        {
            return Result<MyWorkExtendedDto>.Success(Empty() with { Waiting = waiting });
        }

        var itemIds = all.Select(i => i.Id).ToList();
        var boardIds = all.Select(i => i.BoardId).Distinct().ToList();

        var semanticFields = await LoadSemanticFieldsAsync(boardIds, cancellationToken);
        var fieldIds = semanticFields.Values
            .SelectMany(f => new[] { f.Status?.Id, f.Priority?.Id })
            .OfType<Guid>()
            .ToList();
        var values = fieldIds.Count == 0
            ? []
            : await context.BoardItemValues
                .AsNoTracking()
                .Where(v => itemIds.Contains(v.ItemId) && fieldIds.Contains(v.FieldId))
                .Select(v => new { v.ItemId, v.FieldId, v.Value })
                .ToListAsync(cancellationToken);
        var optionByItemField = values
            .GroupBy(v => (v.ItemId, v.FieldId))
            .ToDictionary(g => g.Key, g => WorkItemSemanticFields.ReadOptionId(g.First().Value));

        // The person in charge is stated only when it is someone other than the caller.
        var firstAssignee = (await context.BoardItemMembers
                .AsNoTracking()
                .Where(m => itemIds.Contains(m.ItemId) && m.UserId != userId)
                .Select(m => new { m.ItemId, m.UserId, m.AssignedAt })
                .ToListAsync(cancellationToken))
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.AssignedAt).ThenBy(m => m.UserId).First().UserId);

        var people = (await actors.FindManyAsync(
                firstAssignee.Values.Concat(approvals.Values.Select(a => a.RequestedBy)).Distinct().ToList(), cancellationToken))
            .ToDictionary(a => a.UserId, a => new MyWorkPersonDto(a.UserId, a.Name));

        var assignedIds = open.Concat(done).Select(i => i.Id).ToHashSet();

        MyWorkItemExtDto Map(BoardItem item)
        {
            semanticFields.TryGetValue(item.BoardId, out var fields);
            return new MyWorkItemExtDto(
                item.Id,
                item.BoardId,
                item.GroupId,
                item.ItemKey,
                item.Name,
                item.DueAt,
                item.StartedAt,
                item.UpdatedAt,
                item.Version,
                StatusOf(item, fields?.Status, optionByItemField),
                PriorityOf(item, fields?.Priority, optionByItemField),
                RelationsOf(item.Id),
                firstAssignee.TryGetValue(item.Id, out var assigneeId) && people.TryGetValue(assigneeId, out var person) ? person : null,
                approvals.TryGetValue(item.Id, out var approval)
                    ? new MyWorkApprovalDto(
                        approval.RequestId,
                        people.TryGetValue(approval.RequestedBy, out var requester)
                            ? requester
                            : new MyWorkPersonDto(approval.RequestedBy, string.Empty),
                        approval.RequestedAt)
                    : null,
                item.CompletedAt);
        }

        IReadOnlyList<string> RelationsOf(Guid itemId)
        {
            var relations = new List<string>();
            if (assignedIds.Contains(itemId)) relations.Add("assigned");
            if (followedSet.Contains(itemId)) relations.Add("following");
            if (approvals.ContainsKey(itemId)) relations.Add("approver");
            return relations;
        }

        var shownBoardIds = open.Concat(followedOnly).Concat(done).Select(i => i.BoardId).Distinct().ToList();
        // The Space each shown Board is placed in (WM-10); null for a Board at the workspace level.
        var departments = (await context.Boards
                .AsNoTracking()
                .Where(b => shownBoardIds.Contains(b.Id) && b.WorkspaceId == request.WorkspaceId)
                .Select(b => new { b.Id, b.SpaceId })
                .ToListAsync(cancellationToken))
            .ToDictionary(b => b.Id.ToString(), b => b.SpaceId?.ToString());

        return Result<MyWorkExtendedDto>.Success(new MyWorkExtendedDto(
            open.Concat(followedOnly).Select(Map).ToList(),
            done.Select(Map).ToList(),
            waiting,
            departments));
    }

    /// <summary>
    /// The open Items the caller created or follows, from Boards they may read, where the caller is not
    /// assigned and someone else is. The person in charge is the one assigned first.
    /// </summary>
    private async Task<IReadOnlyList<MyWorkWaitingDto>> LoadWaitingAsync(
        Guid workspaceId,
        Guid userId,
        IReadOnlyCollection<Guid> visibleBoardIds,
        IReadOnlySet<Guid> followedIds,
        CancellationToken cancellationToken)
    {
        var followed = followedIds.ToList();
        var items = await (
                from item in context.BoardItems.AsNoTracking()
                join board in context.Boards.AsNoTracking() on item.BoardId equals board.Id
                where item.WorkspaceId == workspaceId
                      && board.WorkspaceId == workspaceId
                      && item.DeletedAt == null
                      && !item.IsArchived
                      && item.CompletedAt == null
                      && board.DeletedAt == null
                      && !board.IsArchived
                      && visibleBoardIds.Contains(board.Id)
                      && (item.CreatedBy == userId || followed.Contains(item.Id))
                      && context.BoardItemMembers.Any(m => m.ItemId == item.Id && m.UserId != userId)
                      && !context.BoardItemMembers.Any(m => m.ItemId == item.Id && m.UserId == userId)
                select item)
            .OrderBy(i => i.DueAt == null)
            .ThenBy(i => i.DueAt)
            .ThenBy(i => i.Id)
            .Take(GetMyWorkExtendedQuery.WaitingLimit)
            .ToListAsync(cancellationToken);
        if (items.Count == 0) return [];

        var itemIds = items.Select(i => i.Id).ToList();
        var owners = (await context.BoardItemMembers
                .AsNoTracking()
                .Where(m => itemIds.Contains(m.ItemId) && m.UserId != userId)
                .Select(m => new { m.ItemId, m.UserId, m.AssignedAt })
                .ToListAsync(cancellationToken))
            .GroupBy(m => m.ItemId)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.AssignedAt).ThenBy(m => m.UserId).First().UserId);
        var reminded = await reminders.LastRemindedAsync(userId, itemIds, cancellationToken);
        var people = (await actors.FindManyAsync(owners.Values.Distinct().ToList(), cancellationToken))
            .ToDictionary(a => a.UserId, a => new MyWorkPersonDto(a.UserId, a.Name));

        return items
            .Where(i => owners.TryGetValue(i.Id, out var owner) && people.ContainsKey(owner))
            .Select(i => new MyWorkWaitingDto(
                i.Id,
                i.BoardId,
                i.ItemKey,
                i.Name,
                people[owners[i.Id]],
                i.DueAt,
                reminded.TryGetValue(i.Id, out var at) ? at : null))
            .ToList();
    }

    private static MyWorkExtendedDto Empty() => new([], [], [], new Dictionary<string, string?>());

    private sealed record BoardSemanticFields(BoardField? Status, BoardField? Priority);

    private async Task<Dictionary<Guid, BoardSemanticFields>> LoadSemanticFieldsAsync(
        IReadOnlyCollection<Guid> boardIds, CancellationToken cancellationToken)
    {
        var fields = await context.BoardFields
            .AsNoTracking()
            .Include(f => f.Options)
            .Where(f => boardIds.Contains(f.BoardId)
                        && f.DeletedAt == null
                        && (f.Type == FieldType.Status || f.Type == FieldType.Select))
            .ToListAsync(cancellationToken);

        return fields
            .GroupBy(f => f.BoardId)
            .ToDictionary(
                g => g.Key,
                g => new BoardSemanticFields(
                    WorkItemSemanticFields.ResolveStatusField(g),
                    WorkItemSemanticFields.ResolvePriorityField(g)));
    }

    private static MyWorkStatusDto? StatusOf(
        BoardItem item, BoardField? field, IReadOnlyDictionary<(Guid, Guid), Guid?> optionByItemField)
    {
        if (field is null || !optionByItemField.TryGetValue((item.Id, field.Id), out var optionId) || optionId is null)
            return null;
        var option = field.Options.FirstOrDefault(o => o.Id == optionId);
        return option is null ? null : new MyWorkStatusDto(option.Id, option.Name, BucketOf(option.Category));
    }

    private static MyWorkPriorityDto? PriorityOf(
        BoardItem item, BoardField? field, IReadOnlyDictionary<(Guid, Guid), Guid?> optionByItemField)
    {
        if (field is null || !optionByItemField.TryGetValue((item.Id, field.Id), out var optionId) || optionId is null)
            return null;
        var option = field.Options.FirstOrDefault(o => o.Id == optionId);
        return option is null ? null : new MyWorkPriorityDto(option.Id, option.Name);
    }

    private static string BucketOf(WorkStatusCategory? category) => category switch
    {
        WorkStatusCategory.InProgress => "doing",
        WorkStatusCategory.Review => "review",
        WorkStatusCategory.Done => "done",
        _ => "todo",
    };
}

public class GetMyWorkExtendedQueryValidator : AbstractValidator<GetMyWorkExtendedQuery>
{
    public GetMyWorkExtendedQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}
