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

    public ListSavedFiltersQueryHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<SavedFilterDto>>> Handle(ListSavedFiltersQuery request, CancellationToken ct)
    {
        var filters = await _context.SavedFilters
            .AsNoTracking()
            .Where(f => f.BoardId == request.BoardId && !f.IsDeleted)
            .OrderBy(f => f.CreatedAt)
            .Select(f => new SavedFilterDto(
                f.Id,
                f.Name,
                f.BoardId,
                f.ViewId,
                f.Visibility.ToString(),
                f.CreatedAt))
            .ToListAsync(ct);

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
