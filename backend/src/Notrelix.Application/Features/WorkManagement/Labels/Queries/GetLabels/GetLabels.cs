using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Labels.Queries.GetLabels;

public record GetLabelsQuery(Guid BoardId) : IQuery<Result<List<BoardItemLabelDto>>>;

public class GetLabelsQueryHandler : IRequestHandler<GetLabelsQuery, Result<List<BoardItemLabelDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetLabelsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<BoardItemLabelDto>>> Handle(GetLabelsQuery request, CancellationToken ct)
    {
        var labels = await _context.Labels.AsNoTracking()
            .Where(l => l.BoardId == request.BoardId)
            .Select(l => new BoardItemLabelDto(l.Id, l.Name, l.Color.Hex))
            .ToListAsync(ct);

        return Result<List<BoardItemLabelDto>>.Success(labels);
    }
}
