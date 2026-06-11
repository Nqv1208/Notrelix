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
        var items = await _context.BoardItems
            .AsNoTracking()
            .Include(item => item.Members)
            .Include(item => item.Labels)
            .Where(item => item.Group.BoardId == request.BoardId && !item.IsDeleted)
            .OrderBy(item => item.Position)
            .ToListAsync(cancellationToken);

        return items.Select(item => new BoardItemSlimDto(
            item.Id,
            item.GroupId,
            item.Title,
            item.DescriptionMd,
            item.Position,
            item.Priority?.ToString(),
            item.Status.ToString(),
            item.DueDate,
            item.StartDate,
            item.ValuesJson,
            item.Members.Select(m => m.UserId).ToList(),
            item.Labels.Select(l => l.LabelId).ToList()
        )).ToList();
    }
}
