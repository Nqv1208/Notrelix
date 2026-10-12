using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardAccess;

/// <summary>
/// The caller's effective level on a Board, its source, and the people who can
/// reach the Board. Authorized as a single Board read; a Board the caller
/// cannot see is NotFound, exactly as a missing one (GOV-025).
/// </summary>
public record GetBoardAccessQuery(Guid BoardId) : IQuery<Result<BoardAccessDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class GetBoardAccessQueryHandler : IRequestHandler<GetBoardAccessQuery, Result<BoardAccessDto>>
{
    private readonly IResourceAccessSummary _access;
    private readonly IBoardAccessSubjectFacts _boards;
    private readonly ICurrentRequestContext _requestContext;

    public GetBoardAccessQueryHandler(
        IResourceAccessSummary access,
        IBoardAccessSubjectFacts boards,
        ICurrentRequestContext requestContext)
    {
        _access = access;
        _boards = boards;
        _requestContext = requestContext;
    }

    public async Task<Result<BoardAccessDto>> Handle(GetBoardAccessQuery request, CancellationToken ct)
    {
        var board = await _boards.FindAsync(_requestContext.RequireWorkspaceId(), request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var summary = await _access.GetBoardAccessAsync(request.BoardId, ct)
            ?? throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        return Result<BoardAccessDto>.Success(new BoardAccessDto(
            board.BoardId,
            board.Version,
            summary.MyLevel,
            new BoardAccessSourceDto(
                summary.MySource.Kind,
                summary.MySource.GrantedBy is { } grantor ? new BoardAccessGrantorDto(grantor.UserId, grantor.Name) : null),
            summary.PeopleCount,
            summary.GuestCount,
            summary.PeopleCountComplete,
            summary.Sources.Select(s => new BoardAccessSourceCountDto(s.Kind, s.Id, s.Name, s.Count)).ToList()));
    }
}
