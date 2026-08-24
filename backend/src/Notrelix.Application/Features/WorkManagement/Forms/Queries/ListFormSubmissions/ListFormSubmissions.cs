using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Forms.Queries.ListFormSubmissions;

public record ListFormSubmissionsQuery(Guid FormId) : IQuery<Result<List<FormSubmissionDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
}

public class ListFormSubmissionsQueryHandler : IRequestHandler<ListFormSubmissionsQuery, Result<List<FormSubmissionDto>>>
{
    private readonly IWorkManagementDbContext _context;

    public ListFormSubmissionsQueryHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FormSubmissionDto>>> Handle(ListFormSubmissionsQuery request, CancellationToken ct)
    {
        var submissions = await _context.FormSubmissions
            .AsNoTracking()
            .Where(s => s.FormId == request.FormId && s.Status != FormSubmissionStatus.Deleted)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new FormSubmissionDto(
                s.Id,
                s.FormId,
                s.BoardId,
                s.CreatedItemId,
                s.SubmitterUserId,
                s.SubmitterEmail,
                s.Status.ToString(),
                s.SubmittedAt,
                s.ProcessedAt))
            .ToListAsync(ct);

        return Result<List<FormSubmissionDto>>.Success(submissions);
    }
}

public class ListFormSubmissionsQueryValidator : AbstractValidator<ListFormSubmissionsQuery>
{
    public ListFormSubmissionsQueryValidator()
    {
        RuleFor(x => x.FormId).NotEmpty();
    }
}
