using MediatR;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Queries;

public record GetBoardItemsQuery(Guid WorkspaceId, Guid BoardId) : IRequest<List<BoardItemSlimDto>>, IAuthorizeableRequest
{
    public ResourceType ResourceType => ResourceType.Board;
    public Guid ResourceId => BoardId;
    public PermissionAction Action => PermissionAction.ViewBoard;
}

public class GetBoardItemsQueryHandler : IRequestHandler<GetBoardItemsQuery, List<BoardItemSlimDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBoardItemsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BoardItemSlimDto>> Handle(GetBoardItemsQuery request, CancellationToken cancellationToken)
    {
        var boardGroupIds = await _context.BoardGroups
            .AsNoTracking()
            .Where(g => g.BoardId == request.BoardId)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        var items = await _context.BoardItems
            .AsNoTracking()
            .Where(item => boardGroupIds.Contains(item.GroupId) && !item.IsDeleted)
            .OrderBy(item => item.Position)
            .ToListAsync(cancellationToken);

        var itemIds = items.Select(item => item.Id).ToList();

        var memberIds = await _context.BoardItemMembers
            .AsNoTracking()
            .Where(m => itemIds.Contains(m.ItemId))
            .GroupBy(m => m.ItemId)
            .Select(g => new { ItemId = g.Key, UserIds = g.Select(m => m.UserId).ToList() })
            .ToDictionaryAsync(x => x.ItemId, x => x.UserIds, cancellationToken);

        var labelIds = await _context.BoardItemLabels
            .AsNoTracking()
            .Where(l => itemIds.Contains(l.ItemId))
            .GroupBy(l => l.ItemId)
            .Select(g => new { ItemId = g.Key, LabelIds = g.Select(l => l.LabelId).ToList() })
            .ToDictionaryAsync(x => x.ItemId, x => x.LabelIds, cancellationToken);

        return items.Select(item => new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Name,
            item.Position.Value,
            memberIds.GetValueOrDefault(item.Id) ?? [],
            labelIds.GetValueOrDefault(item.Id) ?? []
        )).ToList();
    }
}
