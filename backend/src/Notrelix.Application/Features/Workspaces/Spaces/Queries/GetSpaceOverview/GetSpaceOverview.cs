using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Public.SpaceOverview;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.BoardWork;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceOverview;

public record SpaceOverviewPersonDto(Guid Id, string Name);

/// <param name="Done">Completed top-level Items.</param>
/// <param name="Total">All top-level Items.</param>
public record SpaceOverviewProgressDto(int Done, int Total);

/// <param name="Overdue">Unfinished Items past their due time.</param>
/// <param name="WaitingReview">Pending approval requests on the Board or on its unfinished Items.</param>
/// <param name="Unclaimed">Unfinished top-level Items nobody is assigned to.</param>
public record SpaceOverviewBoardAttentionDto(int Overdue, int WaitingReview, int Unclaimed);

/// <param name="Kind"><c>kanban</c>, <c>table</c> or <c>inbox</c>.</param>
/// <param name="AddedCount">People who are members of a Private Board; null for any other Board.</param>
/// <param name="Progress">Top-level Items done of total; null when the Board has no top-level Item.</param>
/// <param name="Attention">Null when nothing needs attention.</param>
/// <param name="People">Up to three people with the most unfinished Items on the Board.</param>
/// <param name="UpdatedAt">Latest change of the Board or of any of its Items.</param>
public record SpaceOverviewBoardDto(
    Guid Id,
    string Name,
    string Kind,
    bool IsPrivate,
    int? AddedCount,
    SpaceOverviewProgressDto? Progress,
    SpaceOverviewBoardAttentionDto? Attention,
    IReadOnlyList<SpaceOverviewPersonDto> People,
    DateTimeOffset UpdatedAt);

/// <param name="EditorName">Who made the last change (the creator when never edited).</param>
public record SpaceOverviewDocDto(Guid Id, string Title, string? EditorName, DateTimeOffset UpdatedAt);

/// <param name="Id"><c>overdue</c>, <c>unassigned</c> (unclaimed requests on inbox Boards), <c>review</c> or <c>stale</c> (running Boards untouched for 14 days).</param>
/// <param name="BoardId">The Board holding most of the count (the stalest Board for <c>stale</c>).</param>
public record SpaceOverviewAttentionDto(string Id, int Count, Guid? BoardId);

public record SpaceOverviewWorkloadDto(SpaceOverviewPersonDto Person, int Active, int Overdue);

/// <param name="People">People the child Space reaches (Teams expanded, each once).</param>
/// <param name="Boards">Readable active Boards placed in the child Space.</param>
public record SpaceOverviewChildDto(Guid Id, int People, int Boards);

public record SpaceOverviewTeamDto(string Name, int Count);

/// <param name="People">People whose highest role is this one, each once, Teams expanded.</param>
public record SpaceOverviewRoleDto(int People, IReadOnlyList<SpaceOverviewTeamDto> Teams);

public record SpaceOverviewGuestBoardDto(Guid Id, string Name);

public record SpaceOverviewGuestDto(Guid Id, string Name, IReadOnlyList<SpaceOverviewGuestBoardDto> Boards);

/// <param name="Guests">Workspace Guests with access to readable Boards of the Space; empty when the caller may not see Board entries and in the member preview.</param>
public record SpaceOverviewAccessDto(
    IReadOnlyList<SpaceOverviewPersonDto> Admins,
    SpaceOverviewRoleDto Members,
    SpaceOverviewRoleDto Viewers,
    IReadOnlyList<SpaceOverviewGuestDto> Guests);

/// <param name="Boards">The running Boards, most recently changed first, at most six; finished Boards are counted only in <paramref name="BoardTotal"/>.</param>
/// <param name="BoardTotal">Readable Boards of the Space that are not archived.</param>
public record SpaceOverviewDto(
    int PeopleTotal,
    IReadOnlyList<SpaceOverviewPersonDto> People,
    int BoardTotal,
    IReadOnlyList<SpaceOverviewBoardDto> Boards,
    int DocTotal,
    IReadOnlyList<SpaceOverviewDocDto> Docs,
    IReadOnlyList<SpaceOverviewAttentionDto> Attention,
    IReadOnlyList<SpaceOverviewWorkloadDto> Workload,
    IReadOnlyList<SpaceOverviewChildDto> Children,
    SpaceOverviewAccessDto Access);

/// <summary>
/// The department overview (CTR-GAP-WS-SPACE-OVERVIEW): structured figures the client composes into text.
/// Same visibility as the Space (a Space the caller cannot see is NotFound, GOV-025/GOV-029); a Guest does not
/// enumerate the Workspace, so the read needs <c>ViewMembers</c> (GOV-009). Boards and Pages enter only through
/// the Governance visible id sets and the producers' published facts. <c>AsMember</c> previews what a plain
/// department member sees and only a Space manager may ask (the pipeline requires <c>ManageSpaces</c> on the Space).
/// </summary>
public record GetSpaceOverviewQuery(Guid WorkspaceId, Guid SpaceId, bool AsMember = false)
    : IQuery<Result<SpaceOverviewDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    // The member preview is a management read of the Space (pipeline decides, 403 for a non-manager);
    // the plain read is the Workspace member list read, then the handler applies the Space visibility.
    PermissionAction IRequirePermission.Action => AsMember ? PermissionAction.ManageSpaces : PermissionAction.ViewMembers;
    ResourceRef IRequirePermission.Resource => AsMember
        ? ResourceRef.Create(ResourceKind.Create("workspaces.space"), SpaceId, WorkspaceId)
        : ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetSpaceOverviewQueryHandler : IRequestHandler<GetSpaceOverviewQuery, Result<SpaceOverviewDto>>
{
    public const int MaxBoards = 6;
    public const int MaxDocs = 4;
    public const int MaxWorkload = 5;
    public const int MaxPeople = 4;
    public const int MaxGuestBoards = 50;
    public static readonly TimeSpan StaleAfter = TimeSpan.FromDays(14);

    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceReachReader _reach;
    private readonly IResourceVisibility _visibility;
    private readonly IBoardWorkSummaryFacts _work;
    private readonly IPageSpaceOverviewFacts _pages;
    private readonly IBoardSharingSummary _sharing;
    private readonly IActorLookupService _actors;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public GetSpaceOverviewQueryHandler(
        IWorkspaceDbContext context,
        ISpaceReachReader reach,
        IResourceVisibility visibility,
        IBoardWorkSummaryFacts work,
        IPageSpaceOverviewFacts pages,
        IBoardSharingSummary sharing,
        IActorLookupService actors,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _reach = reach;
        _visibility = visibility;
        _work = work;
        _pages = pages;
        _sharing = sharing;
        _actors = actors;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<SpaceOverviewDto>> Handle(GetSpaceOverviewQuery request, CancellationToken ct)
    {
        var workspaceId = request.WorkspaceId;
        var userId = _requestContext.UserId;
        var now = _clock.UtcNow;

        var visibleSpaces = await _visibility.VisibleSpacesAsync(workspaceId, userId, ct);
        if (visibleSpaces.All(s => s.SpaceId != request.SpaceId))
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var exists = await _context.Spaces
            .AsNoTracking()
            .AnyAsync(s => s.WorkspaceId == workspaceId && s.Id == request.SpaceId && s.DeletedAt == null, ct);
        if (!exists)
            throw new NotFoundException(nameof(Space), request.SpaceId);

        var visibleSpaceIds = visibleSpaces.Select(s => s.SpaceId).ToList();
        var childQuery = _context.Spaces
            .AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId && s.ParentSpaceId == request.SpaceId
                && s.DeletedAt == null && s.Status == SpaceStatus.Active
                && visibleSpaceIds.Contains(s.Id));
        if (request.AsMember)
            childQuery = childQuery.Where(s => s.Visibility != SpaceVisibility.Private);
        var children = await childQuery
            .OrderBy(s => s.Position)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var reach = await _reach.ReadAsync(workspaceId, [request.SpaceId, .. children], ct);
        var self = reach[request.SpaceId];

        var readableBoards = await _visibility.VisibleBoardIdsAsync(workspaceId, userId, ct);
        var includePrivate = !request.AsMember;
        var summaries = await _work.SummarizeAsync(workspaceId, [request.SpaceId], readableBoards, includePrivate, now, ct);
        var childBoardCounts = await _work.CountBoardsBySpaceAsync(workspaceId, children, readableBoards, includePrivate, ct);

        var running = summaries.Where(b => b.IsRunning).OrderByDescending(b => b.LastActivityAt).ThenBy(b => b.BoardId).ToList();
        var shown = running.Take(MaxBoards).ToList();

        var readablePages = await _visibility.VisiblePageIdsAsync(workspaceId, userId, ct);
        var rootPages = await _pages.ReadRootPagesAsync(workspaceId, request.SpaceId, readablePages, MaxDocs, ct);

        var workload = await _work.OpenWorkByPersonAsync(workspaceId, summaries.Select(b => b.BoardId).ToList(), now, MaxWorkload, ct);

        var guests = request.AsMember ? [] : await ReadGuestsAsync(summaries, ct);

        var admins = self.People.Where(p => p.Role is SpaceRole.Admin).ToList();
        var nameIds = new HashSet<Guid>();
        nameIds.UnionWith(admins.Select(p => p.UserId));
        nameIds.UnionWith(self.People.Take(MaxPeople).Select(p => p.UserId));
        nameIds.UnionWith(shown.SelectMany(b => b.TopAssignees).Select(a => a.UserId));
        nameIds.UnionWith(workload.Select(w => w.UserId));
        nameIds.UnionWith(rootPages.Recent.Where(p => p.EditorUserId.HasValue).Select(p => p.EditorUserId!.Value));
        var names = (await _actors.FindManyAsync(nameIds.ToList(), ct)).ToDictionary(a => a.UserId, a => a.Name);
        string NameOf(Guid id) => names.GetValueOrDefault(id) ?? "Unknown";
        SpaceOverviewPersonDto Person(Guid id) => new(id, NameOf(id));

        var boardDtos = shown.Select(b => new SpaceOverviewBoardDto(
            b.BoardId,
            b.Title,
            b.Kind,
            b.IsPrivate,
            b.IsPrivate ? b.MemberCount : null,
            b.TopLevelTotal > 0 ? new SpaceOverviewProgressDto(b.TopLevelDone, b.TopLevelTotal) : null,
            b.Overdue + b.WaitingReview + b.Unclaimed > 0 ? new SpaceOverviewBoardAttentionDto(b.Overdue, b.WaitingReview, b.Unclaimed) : null,
            b.TopAssignees.Select(a => Person(a.UserId)).ToList(),
            b.LastActivityAt)).ToList();

        var staleBefore = now - StaleAfter;
        var attention = new List<SpaceOverviewAttentionDto>();
        void AddAttention(string id, IEnumerable<(Guid BoardId, int Count)> perBoard)
        {
            var list = perBoard.Where(x => x.Count > 0).ToList();
            if (list.Count == 0)
                return;
            var top = list.OrderByDescending(x => x.Count).ThenBy(x => x.BoardId).First();
            attention.Add(new SpaceOverviewAttentionDto(id, list.Sum(x => x.Count), top.BoardId));
        }

        AddAttention("overdue", summaries.Select(b => (b.BoardId, b.Overdue)));
        AddAttention("unassigned", summaries.Where(b => b.Kind == BoardWorkKinds.Inbox).Select(b => (b.BoardId, b.Unclaimed)));
        AddAttention("review", summaries.Select(b => (b.BoardId, b.WaitingReview)));
        var stale = running.Where(b => b.LastActivityAt < staleBefore).OrderBy(b => b.LastActivityAt).ThenBy(b => b.BoardId).ToList();
        if (stale.Count > 0)
            attention.Add(new SpaceOverviewAttentionDto("stale", stale.Count, stale[0].BoardId));

        var members = self.People.Where(p => p.Role is SpaceRole.Member).ToList();
        var viewers = self.People.Where(p => p.Role is SpaceRole.Viewer).ToList();
        SpaceOverviewRoleDto RoleDto(SpaceRole role, int people) => new(
            people,
            self.TeamEntries.Where(t => t.Role.Equals(role)).Select(t => new SpaceOverviewTeamDto(t.Name, t.People)).ToList());

        return Result<SpaceOverviewDto>.Success(new SpaceOverviewDto(
            self.People.Count,
            self.People.Take(MaxPeople).Select(p => Person(p.UserId)).ToList(),
            summaries.Count,
            boardDtos,
            rootPages.Total,
            rootPages.Recent.Select(p => new SpaceOverviewDocDto(
                p.Id, p.Title, p.EditorUserId is { } editor ? NameOf(editor) : null, p.UpdatedAt)).ToList(),
            attention,
            workload.Select(w => new SpaceOverviewWorkloadDto(Person(w.UserId), w.Open, w.Overdue)).ToList(),
            children.Select(id => new SpaceOverviewChildDto(id, reach[id].People.Count, childBoardCounts.GetValueOrDefault(id))).ToList(),
            new SpaceOverviewAccessDto(
                admins.Select(p => Person(p.UserId)).ToList(),
                RoleDto(SpaceRole.Member, members.Count),
                RoleDto(SpaceRole.Viewer, viewers.Count),
                guests)));
    }

    /// <summary>
    /// Workspace Guests on the readable Boards of the Space. Board entries are Governance's disclosure: the
    /// published sharing view lists them only to a Board manager or member, so other callers see no guests.
    /// </summary>
    private async Task<IReadOnlyList<SpaceOverviewGuestDto>> ReadGuestsAsync(IReadOnlyList<BoardWorkSummary> boards, CancellationToken ct)
    {
        var byGuest = new Dictionary<Guid, (string Name, List<SpaceOverviewGuestBoardDto> Boards)>();
        foreach (var board in boards.OrderBy(b => b.Title, StringComparer.Ordinal).ThenBy(b => b.BoardId).Take(MaxGuestBoards))
        {
            var sharing = await _sharing.GetBoardSharingAsync(board.BoardId, ct);
            if (sharing is not { EntriesDisclosed: true })
                continue;
            foreach (var entry in sharing.Entries.Where(e => e.Kind == BoardSharingEntryKinds.Guest))
            {
                if (!byGuest.TryGetValue(entry.Id, out var guest))
                    guest = (entry.Name, []);
                guest.Boards.Add(new SpaceOverviewGuestBoardDto(board.BoardId, board.Title));
                byGuest[entry.Id] = guest;
            }
        }

        return byGuest
            .OrderBy(g => g.Value.Name, StringComparer.Ordinal).ThenBy(g => g.Key)
            .Select(g => new SpaceOverviewGuestDto(g.Key, g.Value.Name, g.Value.Boards))
            .ToList();
    }
}
