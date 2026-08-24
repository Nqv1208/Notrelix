using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Forms.Queries.ListForms;

public record ListFormsQuery(Guid BoardId) : IQuery<Result<List<FormDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListFormsQueryHandler : IRequestHandler<ListFormsQuery, Result<List<FormDto>>>
{
    private readonly IWorkManagementDbContext _context;

    public ListFormsQueryHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FormDto>>> Handle(ListFormsQuery request, CancellationToken ct)
    {
        var forms = await _context.Forms
            .AsNoTracking()
            .Where(f => f.BoardId == request.BoardId && !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FormDto(
                f.Id,
                f.Name,
                f.Slug,
                f.BoardId,
                f.Status.ToString(),
                f.Visibility.ToString(),
                f.CreatedAt))
            .ToListAsync(ct);

        return Result<List<FormDto>>.Success(forms);
    }
}

public class ListFormsQueryValidator : AbstractValidator<ListFormsQuery>
{
    public ListFormsQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
