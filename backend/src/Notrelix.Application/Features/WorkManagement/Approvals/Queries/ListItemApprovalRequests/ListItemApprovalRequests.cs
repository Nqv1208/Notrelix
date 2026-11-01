using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Queries.ListItemApprovalRequests;

/// <summary>
/// The approval requests asked about one Item, newest first, each with its steps (who was asked, whether they
/// decided, when and with what note). Anyone who can read the Item's Board can see them.
/// </summary>
public record ListItemApprovalRequestsQuery(Guid ItemId)
    : IQuery<Result<List<ApprovalRequestDto>>>, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class ListItemApprovalRequestsQueryHandler(IWorkManagementDbContext context)
    : IRequestHandler<ListItemApprovalRequestsQuery, Result<List<ApprovalRequestDto>>>
{
    public async Task<Result<List<ApprovalRequestDto>>> Handle(ListItemApprovalRequestsQuery request, CancellationToken ct)
    {
        var kind = ResourceKind.Create("work-management.board-item");
        var approvals = await context.ApprovalRequests
            .AsNoTracking()
            .Include(a => a.Steps)
            .Where(a => a.Target.Kind == kind && a.Target.ResourceId == request.ItemId && a.DeletedAt == null)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var result = approvals.Select(a => new ApprovalRequestDto(
            a.Id,
            a.Title,
            a.Description,
            a.Status.ToString(),
            a.RequestedByUserId,
            a.CreatedAt,
            a.Steps.OrderBy(s => s.Position).Select(s => new ApprovalStepDto(
                s.Id, s.ApproverUserId, s.ApproverTeamId, s.Status.ToString(), s.Position, s.DecidedAt, s.Note)).ToList(),
            a.Version)).ToList();
        return Result<List<ApprovalRequestDto>>.Success(result);
    }
}

public class ListItemApprovalRequestsQueryValidator : AbstractValidator<ListItemApprovalRequestsQuery>
{
    public ListItemApprovalRequestsQueryValidator() => RuleFor(x => x.ItemId).NotEmpty();
}
