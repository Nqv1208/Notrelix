using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Tenancy;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardPeople;

/// <summary>
/// The people who can see one Board through a Board role or an explicit grant on it (UC-WM-A23, UC-WM-C13).
/// This is the board-scoped people read a Guest uses for the assignee, Person-field and mention pickers: a
/// Guest gets 403 from the Workspace members read, so the pickers read this instead. Authorized as a single
/// Board read (ViewBoard); a Board the caller cannot see is NotFound. Names and avatars only, never emails.
/// </summary>
public record GetBoardPeopleQuery(Guid BoardId) : IQuery<Result<List<BoardPersonDto>>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetBoardPeopleQueryHandler : IRequestHandler<GetBoardPeopleQuery, Result<List<BoardPersonDto>>>
{
    private readonly IBoardSharingSummary _sharing;
    private readonly IActorLookupService _actors;

    public GetBoardPeopleQueryHandler(IBoardSharingSummary sharing, IActorLookupService actors)
    {
        _sharing = sharing;
        _actors = actors;
    }

    public async Task<Result<List<BoardPersonDto>>> Handle(GetBoardPeopleQuery request, CancellationToken ct)
    {
        var people = await _sharing.GetBoardPeopleAsync(request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var ids = people.People.Select(p => p.UserId).ToList();
        var actors = ids.Count == 0
            ? new Dictionary<Guid, ActorSnapshot>()
            : (await _actors.FindManyAsync(ids, ct)).ToDictionary(a => a.UserId);

        var result = people.People
            .Select(p => new BoardPersonDto(
                p.UserId,
                actors.TryGetValue(p.UserId, out var actor) ? actor.Name : "Unknown",
                actors.TryGetValue(p.UserId, out var a) ? a.AvatarUrl : null,
                p.Level,
                p.IsBoardMember,
                p.IsGuest))
            .ToList();

        return Result<List<BoardPersonDto>>.Success(result);
    }
}
