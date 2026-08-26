using global::Notrelix.Application.Common.Models;
using global::Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Queries.GetChecklists;

public record GetChecklistsQuery(Guid BoardItemId) : IQuery<Result<List<ChecklistDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class GetChecklistsQueryHandler : IRequestHandler<GetChecklistsQuery, Result<List<ChecklistDto>>>
{
    private readonly IWorkManagementDbContext _context;
    public GetChecklistsQueryHandler(IWorkManagementDbContext context) => _context = context;

    public async Task<Result<List<ChecklistDto>>> Handle(GetChecklistsQuery request, CancellationToken ct)
    {
        var checklists = await _context.Checklists.AsNoTracking()
            .Where(c => c.ItemId == request.BoardItemId)
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        var result = checklists.Select(c => new ChecklistDto(
            c.Id, c.Title, c.Position.Value,
            _context.ChecklistItems.AsNoTracking()
                .Where(i => i.ChecklistId == c.Id)
                .OrderBy(i => i.Position)
                .Select(i => new ChecklistItemDto(
                    i.Id, i.Title, i.Status.ToString(),
                    i.DueAt.HasValue ? i.DueAt.Value.DateTime : (DateTime?)null,
                    i.AssigneeUserId, i.Position.Value))
                .ToList()
        )).ToList();

        return Result<List<ChecklistDto>>.Success(result);
    }
}
