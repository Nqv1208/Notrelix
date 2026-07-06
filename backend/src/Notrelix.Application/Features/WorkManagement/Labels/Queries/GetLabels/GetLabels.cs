using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Labels.Queries.GetLabels;

public record GetLabelsQuery(Guid BoardId)
    : IQuery<Result<List<BoardItemLabelDto>>>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
}

public class GetLabelsQueryHandler : IRequestHandler<GetLabelsQuery, Result<List<BoardItemLabelDto>>>
{
    private readonly IWorkManagementDbContext _context;
    public GetLabelsQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<List<BoardItemLabelDto>>> Handle(GetLabelsQuery request, CancellationToken ct)
    {
        var labels = await _context.Labels.AsNoTracking()
            .Where(l => l.BoardId == request.BoardId)
            .Select(l => new BoardItemLabelDto(l.Id, l.Name, l.Color.Hex))
            .ToListAsync(ct);

        return Result<List<BoardItemLabelDto>>.Success(labels);
    }
}
