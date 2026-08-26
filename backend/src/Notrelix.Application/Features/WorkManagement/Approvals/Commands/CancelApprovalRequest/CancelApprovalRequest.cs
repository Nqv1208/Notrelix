using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;

[IdempotencyOperation("work-management.approvals.cancel-approval-request.v1")]
public record CancelApprovalRequestCommand(
    Guid RequestId,
    long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.approval-request"), RequestId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class CancelApprovalRequestCommandHandler : IRequestHandler<CancelApprovalRequestCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelApprovalRequestCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CancelApprovalRequestCommand request, CancellationToken ct)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (approvalRequest is null) throw new NotFoundException("ApprovalRequest", request.RequestId);

        approvalRequest.Cancel(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
