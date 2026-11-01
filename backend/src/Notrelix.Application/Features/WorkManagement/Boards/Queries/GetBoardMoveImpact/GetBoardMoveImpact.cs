using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardMoveImpact;

/// <summary>
/// What placing the Board in another Space (or at the Workspace root) would change, read before the move:
/// who loses it, who gains it, and which of the people losing it still hold unfinished Items on it. Authorized
/// as managing the Board and as creating a Board in the target, so a caller without either gets what the move
/// would give (NotFound or Forbidden) and learns no membership. The lists are bounded.
/// </summary>
public record GetBoardMoveImpactQuery(Guid BoardId, Guid? TargetSpaceId)
    : IQuery<Result<BoardMoveImpactDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

/// <param name="LosingCount">People who stop reaching the Board; null when <paramref name="EveryoneLoses"/>.</param>
/// <param name="GainingCount">People who start reaching it; 0 (unknown) when <paramref name="EveryoneGains"/>.</param>
/// <param name="EveryoneGains">The target is the Workspace root: every member who is not a Guest reaches the Board.</param>
/// <param name="EveryoneLoses">The Board sits at the Workspace root today: every member the target Space does not reach loses it.</param>
/// <param name="TargetPeople">People the target Space reaches.</param>
/// <param name="Stranded">Losing people with unfinished Items on the Board, at most <see cref="GetBoardMoveImpactQueryHandler.PeopleLimit"/>.</param>
/// <param name="StrandedCount">All losing people with unfinished Items, also those not listed.</param>
/// <param name="StrandedItems">Distinct unfinished Items held by the people counted in <paramref name="StrandedCount"/>.</param>
/// <param name="StrandedOverdue">Of those, the ones past due.</param>
/// <param name="KeptNames">Up to a few people who keep access on their own entry.</param>
/// <param name="KeptCount">All people who keep access on their own entry.</param>
public sealed record BoardMoveImpactDto(
    Guid BoardId,
    Guid? TargetSpaceId,
    string? TargetSpaceName,
    bool Unchanged,
    int? LosingCount,
    int GainingCount,
    bool EveryoneGains,
    bool EveryoneLoses,
    int TargetPeople,
    IReadOnlyList<BoardMoveStrandedPersonDto> Stranded,
    int StrandedCount,
    int StrandedItems,
    int StrandedOverdue,
    IReadOnlyList<string> KeptNames,
    int KeptCount,
    int KeptTeamCount,
    long BoardVersion);

public sealed record BoardMoveStrandedPersonDto(Guid UserId, string Name, int Items, int Overdue);

public class GetBoardMoveImpactQueryHandler : IRequestHandler<GetBoardMoveImpactQuery, Result<BoardMoveImpactDto>>
{
    public const int PeopleLimit = 50;
    private const int KeptNamesLimit = 5;

    private readonly IBoardSharingSummary _sharing;
    private readonly IBoardSpacePlacement _placement;
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actors;
    private readonly IDateTimeProvider _clock;

    public GetBoardMoveImpactQueryHandler(
        IBoardSharingSummary sharing,
        IBoardSpacePlacement placement,
        IWorkManagementDbContext context,
        IActorLookupService actors,
        IDateTimeProvider clock)
    {
        _sharing = sharing;
        _placement = placement;
        _context = context;
        _actors = actors;
        _clock = clock;
    }

    public async Task<Result<BoardMoveImpactDto>> Handle(GetBoardMoveImpactQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        if (board.SpaceId != request.TargetSpaceId)
        {
            await _placement.AuthorizeTargetAsync(board.WorkspaceId, request.TargetSpaceId, ct);
            if (request.TargetSpaceId is { } spaceId)
                await _placement.EnsureActiveTargetAsync(board.WorkspaceId, spaceId, ct);
        }

        var now = _clock.UtcNow;
        var assignees = await (
            from member in _context.BoardItemMembers.AsNoTracking()
            join item in _context.BoardItems.AsNoTracking() on member.ItemId equals item.Id
            where member.BoardId == request.BoardId
                && item.BoardId == request.BoardId
                && item.DeletedAt == null
                && !item.IsArchived
                && item.CompletedAt == null
            select member.UserId)
            .Distinct()
            .ToListAsync(ct);
        var reach = await _sharing.GetBoardPlacementReachAsync(request.BoardId, request.TargetSpaceId, assignees, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);
        var unchanged = board.SpaceId == request.TargetSpaceId;

        var losing = reach.LosingUserIds;
        var workload = losing.Count == 0
            ? []
            : await (
                from member in _context.BoardItemMembers.AsNoTracking()
                join item in _context.BoardItems.AsNoTracking() on member.ItemId equals item.Id
                where member.BoardId == request.BoardId
                    && losing.Contains(member.UserId)
                    && item.BoardId == request.BoardId
                    && item.DeletedAt == null
                    && !item.IsArchived
                    && item.CompletedAt == null
                select new { member.UserId, item.Id, Overdue = item.DueAt != null && item.DueAt < now })
                .Distinct()
                .ToListAsync(ct);

        var perPerson = workload
            .GroupBy(w => w.UserId)
            .Select(g => new { UserId = g.Key, Items = g.Select(x => x.Id).Distinct().Count(), Overdue = g.Where(x => x.Overdue).Select(x => x.Id).Distinct().Count() })
            .OrderByDescending(p => p.Overdue).ThenByDescending(p => p.Items).ThenBy(p => p.UserId)
            .ToList();
        var listed = perPerson.Take(PeopleLimit).ToList();
        var keptIds = reach.KeptUserIds.Take(KeptNamesLimit).ToList();
        var lookup = listed.Select(p => p.UserId).Concat(keptIds).Distinct().ToList();
        var names = lookup.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _actors.FindManyAsync(lookup, ct)).ToDictionary(a => a.UserId, a => a.Name);

        return Result<BoardMoveImpactDto>.Success(new BoardMoveImpactDto(
            request.BoardId,
            request.TargetSpaceId,
            reach.TargetSpaceName,
            unchanged,
            reach.LosingCount,
            reach.GainingCount,
            reach.Everyone,
            reach.EveryoneLoses,
            reach.TargetPeople,
            listed.Select(p => new BoardMoveStrandedPersonDto(p.UserId, names.GetValueOrDefault(p.UserId) ?? "Unknown", p.Items, p.Overdue)).ToList(),
            perPerson.Count,
            workload.Select(w => w.Id).Distinct().Count(),
            workload.Where(w => w.Overdue).Select(w => w.Id).Distinct().Count(),
            keptIds.Select(id => names.GetValueOrDefault(id)).Where(n => n is not null).Select(n => n!).ToList(),
            reach.KeptUserIds.Count,
            reach.KeptTeamCount,
            board.Version));
    }
}
