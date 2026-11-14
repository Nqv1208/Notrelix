using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Approvals.Services;
using Notrelix.Application.Features.Workspaces.Public.Teams;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.ApproveApprovalRequest;

[IdempotencyOperation("work-management.approvals.approve-approval-request.v1")]
public record ApproveApprovalRequestCommand(
    Guid RequestId,
    string? Note,
    long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.approval-request"), RequestId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class ApproveApprovalRequestCommandHandler : IRequestHandler<ApproveApprovalRequestCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITeamMembershipFacts _teams;
    private readonly IBoardRealtimeNotifier? _realtime;

    public ApproveApprovalRequestCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        ITeamMembershipFacts teams,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _teams = teams;
    }

    public async Task<Result> Handle(ApproveApprovalRequestCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(ApproveApprovalRequestCommand request, CancellationToken ct)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (approvalRequest is null) throw new NotFoundException("ApprovalRequest", request.RequestId);

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;
        var (step, myTeams) = await ApprovalDeciders.FindAsync(approvalRequest, approvalRequest.WorkspaceId, userId, _teams, ct);
        // Only an assigned approver (or a current member of the named Team) learns the state of a request that was already decided.
        if (approvalRequest.Status != ApprovalStatus.Pending && step is not null)
            return Result.Failure(new ApplicationError(
                "work.approval.already-decided", $"This approval is already {approvalRequest.Status}.", ApplicationErrorType.Conflict));
        if (step is not null && step.Status != ApprovalStatus.Pending) step = null;
        if (step is null)
            return Result.Failure(new ApplicationError("work.approval.no-pending-step", "No pending approval step assigned to the current user.", ApplicationErrorType.BusinessRule));

        approvalRequest.Approve(step.Id, userId, now, request.Note, myTeams);
        return Result.Success();
    }
}
