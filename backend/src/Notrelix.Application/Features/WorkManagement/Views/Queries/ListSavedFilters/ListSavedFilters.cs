using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Views.Queries.ListSavedFilters;

public record ListSavedFiltersQuery(Guid BoardId) : IQuery<Result<List<SavedFilterDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListSavedFiltersQueryHandler : IRequestHandler<ListSavedFiltersQuery, Result<List<SavedFilterDto>>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IActorLookupService _actors;
    private readonly IWorkspaceAccessResolver _workspaceAccess;

    public ListSavedFiltersQueryHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IActorLookupService actors,
        IWorkspaceAccessResolver workspaceAccess)
    {
        _context = context;
        _requestContext = requestContext;
        _actors = actors;
        _workspaceAccess = workspaceAccess;
    }

    public async Task<Result<List<SavedFilterDto>>> Handle(ListSavedFiltersQuery request, CancellationToken ct)
    {
        var rows = await _context.SavedFilters
            .AsNoTracking()
            .Where(f => f.BoardId == request.BoardId && f.DeletedAt == null
                && (f.Visibility == SavedFilterVisibility.Public || f.CreatedBy == _requestContext.UserId))
            .OrderBy(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.BoardId,
                f.ViewId,
                f.Visibility,
                f.CreatedAt,
                f.Criteria,
                f.CreatedBy,
                f.Version,
            })
            .ToListAsync(ct);

        // A shared filter names the person who made it, except to a Workspace Guest (the rule of every actor notice).
        var userId = _requestContext.UserId;
        var creators = rows
            .Where(r => r.Visibility == SavedFilterVisibility.Public && r.CreatedBy is { } by && by != userId)
            .Select(r => r.CreatedBy!.Value)
            .Distinct()
            .ToList();
        var names = new Dictionary<Guid, string>();
        if (creators.Count > 0
            && !(await _workspaceAccess.ResolveAsync(_requestContext.RequireWorkspaceId(), userId, ct)).IsGuest)
        {
            foreach (var actor in await _actors.FindManyAsync(creators, ct))
                names[actor.UserId] = actor.Name;
        }

        var filters = rows
            .Select(r => new SavedFilterDto(
                r.Id,
                r.Name,
                r.BoardId,
                r.ViewId,
                r.Visibility.ToString(),
                r.CreatedAt,
                r.Criteria,
                r.CreatedBy == userId,
                r.Version,
                r.CreatedBy is { } by && names.TryGetValue(by, out var name) ? name : null))
            .ToList();

        return Result<List<SavedFilterDto>>.Success(filters);
    }
}

public class ListSavedFiltersQueryValidator : AbstractValidator<ListSavedFiltersQuery>
{
    public ListSavedFiltersQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
