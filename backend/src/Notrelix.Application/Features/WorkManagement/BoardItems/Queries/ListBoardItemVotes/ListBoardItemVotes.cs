using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemVotes;

public record BoardItemVoterDto(Guid UserId, string Name, string? AvatarUrl, DateTimeOffset VotedAt);

public record BoardItemVotesDto(int Count, bool Voted, IReadOnlyList<BoardItemVoterDto> Voters);

/// <summary>Who voted on an Item for a Vote field. Anyone who can view the board sees the voters.</summary>
public record ListBoardItemVotesQuery(Guid BoardItemId, Guid FieldId) : IQuery<Result<BoardItemVotesDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class ListBoardItemVotesQueryHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IActorLookupService actors) : IRequestHandler<ListBoardItemVotesQuery, Result<BoardItemVotesDto>>
{
    public const int MaxVoters = 200;

    public async Task<Result<BoardItemVotesDto>> Handle(ListBoardItemVotesQuery request, CancellationToken ct)
    {
        var itemExists = await context.BoardItems.AsNoTracking().AnyAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct);
        if (!itemExists) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var votes = context.BoardItemVotes.AsNoTracking().Where(v => v.ItemId == request.BoardItemId && v.FieldId == request.FieldId);
        var count = await votes.CountAsync(ct);
        var rows = await votes.OrderBy(v => v.VotedAt).ThenBy(v => v.Id).Take(MaxVoters).Select(v => new { v.UserId, v.VotedAt }).ToListAsync(ct);
        var names = (await actors.FindManyAsync(rows.Select(r => r.UserId).ToList(), ct)).ToDictionary(a => a.UserId);
        var voters = rows.Select(r => new BoardItemVoterDto(
            r.UserId, names.GetValueOrDefault(r.UserId)?.Name ?? "", names.GetValueOrDefault(r.UserId)?.AvatarUrl, r.VotedAt)).ToList();
        return Result<BoardItemVotesDto>.Success(new BoardItemVotesDto(count, rows.Any(r => r.UserId == requestContext.UserId), voters));
    }
}
