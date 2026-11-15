using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Forms.Queries.ListFormSubmissions;

public record CountFormSubmissionsQuery(Guid FormId) : IQuery<Result<FormSubmissionCountsDto>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    // Same right as the list: the counts describe the responses the member may open.
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.form"), FormId);
}

public class CountFormSubmissionsQueryHandler : IRequestHandler<CountFormSubmissionsQuery, Result<FormSubmissionCountsDto>>
{
    private readonly IWorkManagementDbContext _context;

    public CountFormSubmissionsQueryHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<FormSubmissionCountsDto>> Handle(CountFormSubmissionsQuery request, CancellationToken ct)
    {
        // Accepted splits into New and Processed by whether it made an item, as the list's status does.
        var rows = await _context.FormSubmissions
            .AsNoTracking()
            .Where(s => s.FormId == request.FormId && s.Status != FormSubmissionStatus.Deleted)
            .GroupBy(s => new { s.Status, Made = s.CreatedItemId != null })
            .Select(g => new { g.Key.Status, g.Key.Made, Count = g.Count() })
            .ToListAsync(ct);

        int Of(FormSubmissionStatus status, bool? made = null) =>
            rows.Where(r => r.Status == status && (made is null || r.Made == made)).Sum(r => r.Count);

        return Result<FormSubmissionCountsDto>.Success(new FormSubmissionCountsDto(
            Of(FormSubmissionStatus.Accepted, false),
            Of(FormSubmissionStatus.Accepted, true),
            Of(FormSubmissionStatus.Rejected),
            Of(FormSubmissionStatus.Spam)));
    }
}

public class CountFormSubmissionsQueryValidator : AbstractValidator<CountFormSubmissionsQuery>
{
    public CountFormSubmissionsQueryValidator()
    {
        RuleFor(x => x.FormId).NotEmpty();
    }
}
