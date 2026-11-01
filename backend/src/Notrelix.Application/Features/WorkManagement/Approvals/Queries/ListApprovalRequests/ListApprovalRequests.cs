using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Queries.ListApprovalRequests;

/// <param name="After">Reads the requests that follow this one (newest first); absent reads from the first.</param>
/// <param name="Limit">Reads at most this many; absent reads them all.</param>
public record ListApprovalRequestsQuery(Guid BoardId, TimeCursorPosition? After = null, int? Limit = null) : IQuery<Result<List<ApprovalRequestDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
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
        var live = _context.ApprovalRequests
            .AsNoTracking()
            .Where(a => a.Target.ResourceId == request.BoardId && a.DeletedAt == null);
        if (request.After is { } after)
        {
            live = live.Where(a => a.CreatedAt < after.At || (a.CreatedAt == after.At && a.Id.CompareTo(after.Id) < 0));
        }

        var ordered = live.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id);
        var approvals = await (request.Limit is { } limit ? ordered.Take(limit) : ordered).ToListAsync(ct);

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
            stepsByRequest.GetValueOrDefault(a.Id) ?? new List<ApprovalStepDto>(),
            a.Version)).ToList();

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
