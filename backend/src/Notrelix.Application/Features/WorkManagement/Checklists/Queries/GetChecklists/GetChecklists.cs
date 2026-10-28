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
            .Include(c => c.Items)
            .Where(c => c.ItemId == request.BoardItemId)
            .ToListAsync(ct);

        // Ordinal key order with a stable id tie-break, the same canonical order the
        // placement resolver uses (WM-V3-ORDER-002).
        var result = checklists
            .OrderBy(c => c.Position).ThenBy(c => c.Id)
            .Select(c => new ChecklistDto(
                c.Id, c.Title, c.Position.Value, c.Version,
                c.Items
                    .OrderBy(i => i.Position).ThenBy(i => i.Id)
                    .Select(i => new ChecklistItemDto(
                        i.Id, i.Title, i.Status.ToString(),
                        i.DueAt.HasValue ? i.DueAt.Value.UtcDateTime : (DateTime?)null,
                        i.AssigneeUserId, i.Position.Value))
                    .ToList()))
            .ToList();

        return Result<List<ChecklistDto>>.Success(result);
    }
}
