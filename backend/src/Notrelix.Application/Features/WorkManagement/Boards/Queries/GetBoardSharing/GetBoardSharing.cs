using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardSharing;

/// <summary>
/// The Board's sharing view: who holds access given on the Board itself, the sharing log, and who would
/// lose the Board if it went Private. Authorized as a single Board read; a Board the caller cannot see is
/// NotFound, exactly as a missing one (GOV-025). Entries and names are shown only to a Board manager or a
/// Board member; every other reader gets counts only.
/// </summary>
public record GetBoardSharingQuery(Guid BoardId) : IQuery<Result<BoardSharingDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetBoardSharingQueryHandler : IRequestHandler<GetBoardSharingQuery, Result<BoardSharingDto>>
{
    private readonly IBoardSharingSummary _sharing;
    private readonly IWorkManagementDbContext _context;
    private readonly IMemberEmailDisclosure _emails;
    private readonly IActorLookupService _actors;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public GetBoardSharingQueryHandler(
        IBoardSharingSummary sharing,
        IWorkManagementDbContext context,
        IMemberEmailDisclosure emails,
        IActorLookupService actors,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _sharing = sharing;
        _context = context;
        _emails = emails;
        _actors = actors;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<BoardSharingDto>> Handle(GetBoardSharingQuery request, CancellationToken ct)
    {
        var sharing = await _sharing.GetBoardSharingAsync(request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var workspaceId = _requestContext.RequireWorkspaceId();
        var personIds = sharing.Entries
            .Where(e => e.Kind == BoardSharingEntryKinds.Person)
            .Select(e => e.Id)
            .ToList();
        var emails = await _emails.ResolveAsync(workspaceId, personIds, ct);

        var entries = sharing.Entries
            .Select(e => new BoardSharingEntryDto(
                e.Id,
                e.Kind,
                e.Name,
                e.Kind == BoardSharingEntryKinds.Person ? emails.GetValueOrDefault(e.Id) : null,
                e.You,
                e.Owner,
                e.Level,
                e.LevelCapped,
                e.Source,
                e.PermissionId,
                e.MemberRole,
                e.SpaceRole,
                e.OutsideSpace,
                e.TeamPeople,
                null,
                e.AddedAt,
                e.GrantedBy is { } grantor ? new BoardAccessGrantorDto(grantor.UserId, grantor.Name) : null))
            .ToList();

        var log = sharing.Log
            .Select(l => new BoardSharingLogEntryDto(
                l.Id,
                l.Kind,
                l.Actor is { } actor ? new BoardAccessGrantorDto(actor.UserId, actor.Name) : null,
                l.Target,
                l.From,
                l.To,
                l.Guest,
                l.At))
            .ToList();

        var impact = sharing.CanManage
            ? await BuildImpactAsync(request.BoardId, sharing, ct)
            : null;

        return Result<BoardSharingDto>.Success(new BoardSharingDto(
            request.BoardId,
            sharing.CanManage,
            sharing.EntriesDisclosed,
            sharing.Visibility,
            sharing.SpaceId,
            sharing.SpaceName,
            sharing.SpacePeople,
            sharing.PeopleCount,
            sharing.GuestCount,
            sharing.PeopleCountComplete,
            entries,
            log,
            impact));
    }

    /// <summary>
    /// Who loses the Board on Private is Governance's fact (people who reach it only through the Space);
    /// their unfinished and overdue Items on this Board are Work Management's own.
    /// </summary>
    private async Task<BoardPrivateImpactDto> BuildImpactAsync(Guid boardId, BoardSharing sharing, CancellationToken ct)
    {
        var losing = sharing.LosingUserIds;
        var now = _clock.UtcNow;
        var workload = losing.Count == 0
            ? []
            : await (
                from member in _context.BoardItemMembers.AsNoTracking()
                join item in _context.BoardItems.AsNoTracking() on member.ItemId equals item.Id
                where member.BoardId == boardId
                    && losing.Contains(member.UserId)
                    && item.BoardId == boardId
                    && item.DeletedAt == null
                    && !item.IsArchived
                    && item.CompletedAt == null
                group item by member.UserId into perPerson
                select new
                {
                    UserId = perPerson.Key,
                    Items = perPerson.Select(i => i.Id).Distinct().Count(),
                    Overdue = perPerson.Where(i => i.DueAt != null && i.DueAt < now).Select(i => i.Id).Distinct().Count(),
                })
                .ToListAsync(ct);

        var names = workload.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _actors.FindManyAsync(workload.Select(w => w.UserId).ToList(), ct)).ToDictionary(a => a.UserId, a => a.Name);

        var people = workload
            .Select(w => new BoardPrivateImpactPersonDto(w.UserId, names.GetValueOrDefault(w.UserId) ?? "Unknown", w.Items, w.Overdue))
            .OrderByDescending(p => p.Overdue)
            .ThenByDescending(p => p.Items)
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

        return new BoardPrivateImpactDto(losing.Count, people, sharing.Entries.Select(e => e.Name).ToList());
    }
}
