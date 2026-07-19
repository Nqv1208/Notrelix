using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Queries.ListApprovalRequests;

public record ListApprovalRequestsQuery(Guid BoardId) : IQuery<Result<List<ApprovalRequestDto>>>, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
}

public class ListApprovalRequestsQueryHandler : IRequestHandler<ListApprovalRequestsQuery, Result<List<ApprovalRequestDto>>>
{
    private readonly IWorkManagementDbContext _context;

    public ListApprovalRequestsQueryHandler(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<ApprovalRequestDto>>> Handle(ListApprovalRequestsQuery request, CancellationToken ct)
    {
        var approvals = await _context.ApprovalRequests
            .AsNoTracking()
            .Where(a => a.Target.ResourceId == request.BoardId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var requestIds = approvals.Select(a => a.Id).ToList();

        var steps = await _context.ApprovalSteps
            .AsNoTracking()
            .Where(s => requestIds.Contains(s.ApprovalRequestId))
            .OrderBy(s => s.Position)
            .ToListAsync(ct);

        var stepsByRequest = steps
            .GroupBy(s => s.ApprovalRequestId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(s => new ApprovalStepDto(
                    s.Id,
                    s.ApproverUserId,
                    s.ApproverTeamId,
                    s.Status.ToString(),
                    s.Position,
                    s.DecidedAt,
                    s.Note)).ToList());

        var result = approvals.Select(a => new ApprovalRequestDto(
            a.Id,
            a.Title,
            a.Description,
            a.Status.ToString(),
            a.RequestedByUserId,
            a.CreatedAt,
            stepsByRequest.GetValueOrDefault(a.Id) ?? new List<ApprovalStepDto>())).ToList();

        return Result<List<ApprovalRequestDto>>.Success(result);
    }
}

public class ListApprovalRequestsQueryValidator : AbstractValidator<ListApprovalRequestsQuery>
{
    public ListApprovalRequestsQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
    }
}
