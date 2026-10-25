using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyWorkExtended;

/// <summary>
/// "My work" as the member's page draws it: the open Items assigned to the caller (soonest due first, undated
/// last) and the Items the caller recently completed, each with its key, Status (label and work-status
/// category), Priority option and the person in charge when it is someone else. Both lists are bounded and come only from Boards the
/// caller may read (the single-read ViewBoard decision, GOV-029). The open Items the caller follows are listed
/// too, with the "following" relation. Approvals and "waiting on" have no source yet and are returned empty; <c>departments</c> is empty because no Board-to-department
/// mapping exists.
/// </summary>
public record GetMyWorkExtendedQuery(Guid WorkspaceId)
    : IQuery<Result<MyWorkExtendedDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public const int OpenLimit = 200;
    public const int DoneLimit = 50;

    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public sealed record MyWorkPersonDto(Guid Id, string Name);

/// <summary>A Status option held by an Item: its label and the work-status category it belongs to.</summary>
public sealed record MyWorkStatusDto(Guid OptionId, string Label, string Bucket);

/// <summary>A Priority option held by an Item; the label is the option's own name.</summary>
public sealed record MyWorkPriorityDto(Guid OptionId, string Label);

public sealed record MyWorkApprovalDto(MyWorkPersonDto RequestedBy, DateTimeOffset RequestedAt);

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
    string Note,
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
    IWorkManagementCollaborationReadPort collaboration) : IRequestHandler<GetMyWorkExtendedQuery, Result<MyWorkExtendedDto>>
{
    private static readonly IReadOnlyList<string> AssignedRelation = ["assigned"];
    private static readonly IReadOnlyList<string> FollowingRelation = ["following"];
    private static readonly IReadOnlyList<string> AssignedAndFollowingRelations = ["assigned", "following"];

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

        var all = open.Concat(followedOnly).Concat(done).ToList();
        if (all.Count == 0)
        {
            return Result<MyWorkExtendedDto>.Success(Empty());
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

        var people = (await actors.FindManyAsync(firstAssignee.Values.Distinct().ToList(), cancellationToken))
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
                followedSet.Contains(item.Id)
                    ? (assignedIds.Contains(item.Id) ? AssignedAndFollowingRelations : FollowingRelation)
                    : AssignedRelation,
                firstAssignee.TryGetValue(item.Id, out var assigneeId) && people.TryGetValue(assigneeId, out var person) ? person : null,
                null,
                item.CompletedAt);
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
            [],
            departments));
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
