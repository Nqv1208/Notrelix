using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.RejectApprovalRequest;

[IdempotencyOperation("work-management.approvals.reject-approval-request.v1")]
public record RejectApprovalRequestCommand(
    Guid RequestId,
    string? Note,
    long ExpectedVersion,
    string? IdempotencyKey = null)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.ApprovalRequest, RequestId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"reject-approval:{RequestId}";
}

public class RejectApprovalRequestCommandHandler : IRequestHandler<RejectApprovalRequestCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RejectApprovalRequestCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RejectApprovalRequestCommand request, CancellationToken ct)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (approvalRequest is null) throw new NotFoundException("ApprovalRequest", request.RequestId);

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;
        var step = approvalRequest.Steps
            .FirstOrDefault(s => s.Status == ApprovalStatus.Pending && s.ApproverUserId == userId);
        if (step is null)
            return Result.Failure(new ApplicationError("work.approval.no-pending-step", "No pending approval step assigned to the current user.", ApplicationErrorType.BusinessRule));

        approvalRequest.Reject(step.Id, userId, now, request.Note);
        return Result.Success();
    }
}
