using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Notrelix.Application.Common.Abstractions;
using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.DTOs;
using global::Notrelix.Domain.Identity;
using global::Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.WorkManagement.Queries.GetChecklists;

public record GetChecklistsQuery(Guid BoardItemId) : IRequest<Result<List<ChecklistDto>>>;

public class GetChecklistsQueryHandler : IRequestHandler<GetChecklistsQuery, Result<List<ChecklistDto>>>
{
    private readonly IApplicationDbContext _context;
    public GetChecklistsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<ChecklistDto>>> Handle(GetChecklistsQuery request, CancellationToken ct)
    {
        var checklists = await _context.Checklists.AsNoTracking()
            .Where(c => c.BoardItemId == request.BoardItemId)
            .OrderBy(c => c.Position)
            .Select(c => new ChecklistDto(
                c.Id, c.Title, c.Position,
                _context.ChecklistItems.AsNoTracking()
                    .Where(i => i.ChecklistId == c.Id)
                    .OrderBy(i => i.Position)
                    .Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsChecked, i.DueDate, i.AssigneeId, i.Position))
                    .ToList()
            )).ToListAsync(ct);

        return Result<List<ChecklistDto>>.Success(checklists);
    }
}
