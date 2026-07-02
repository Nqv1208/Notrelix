using BoardEntity = global::Notrelix.Domain.WorkManagement.Boards.Board;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardMembers;

public record GetBoardMembersQuery(Guid WorkspaceId, Guid BoardId) : IQuery<Result<List<BoardMemberDto>>>, IRequirePermission, IWorkspaceRequest
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId, WorkspaceId);
}

public class GetBoardMembersQueryHandler : IRequestHandler<GetBoardMembersQuery, Result<List<BoardMemberDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IActorLookupService _actorLookup;

    public GetBoardMembersQueryHandler(IWorkManagementDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
    }

    public async Task<Result<List<BoardMemberDto>>> Handle(GetBoardMembersQuery request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(BoardEntity), request.BoardId);

        var memberEntities = await _context.BoardMembers.AsNoTracking()
            .Where(m => m.BoardId == request.BoardId)
            .ToListAsync(ct);

        var userIds = memberEntities.Select(m => m.UserId).Distinct().ToList();
        var actors = await _actorLookup.FindManyAsync(userIds, ct);
        var actorMap = actors.ToDictionary(a => a.UserId);

        var members = memberEntities
            .Select(m => new BoardMemberDto(
                m.UserId,
                actorMap.TryGetValue(m.UserId, out var actor) ? actor.Name : "Unknown",
                actorMap.TryGetValue(m.UserId, out var a) ? a.AvatarUrl : null,
                m.Role.ToString(),
                m.JoinedAt.DateTime))
            .ToList();

        return Result<List<BoardMemberDto>>.Success(members);
    }
}
