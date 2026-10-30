using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemViewers;

public record BoardItemViewerDto(Guid UserId, string Name, string? AvatarUrl, DateTimeOffset LastViewedAt);

public record BoardItemViewersDto(int Count, IReadOnlyList<BoardItemViewerDto> Viewers);

/// <summary>Who has opened an Item and when they last did, most recent first. Anyone who can view the board sees it.</summary>
public record ListBoardItemViewersQuery(Guid BoardItemId) : IQuery<Result<BoardItemViewersDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class ListBoardItemViewersQueryHandler(
    IWorkManagementDbContext context,
    IActorLookupService actors) : IRequestHandler<ListBoardItemViewersQuery, Result<BoardItemViewersDto>>
{
    public const int MaxViewers = 100;

    public async Task<Result<BoardItemViewersDto>> Handle(ListBoardItemViewersQuery request, CancellationToken ct)
    {
        var itemExists = await context.BoardItems.AsNoTracking().AnyAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct);
        if (!itemExists) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var views = context.BoardItemViews.AsNoTracking().Where(v => v.ItemId == request.BoardItemId);
        var count = await views.CountAsync(ct);
        var rows = await views.OrderByDescending(v => v.LastViewedAt).ThenBy(v => v.Id).Take(MaxViewers)
            .Select(v => new { v.UserId, v.LastViewedAt }).ToListAsync(ct);
        var names = (await actors.FindManyAsync(rows.Select(r => r.UserId).ToList(), ct)).ToDictionary(a => a.UserId);
        var viewers = rows.Select(r => new BoardItemViewerDto(
            r.UserId, names.GetValueOrDefault(r.UserId)?.Name ?? "", names.GetValueOrDefault(r.UserId)?.AvatarUrl, r.LastViewedAt)).ToList();
        return Result<BoardItemViewersDto>.Success(new BoardItemViewersDto(count, viewers));
    }
}
