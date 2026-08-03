using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.RestoreApprovalRequest;

[IdempotencyOperation("work-management.approvals.restore-approval-request.v1")]
public record RestoreApprovalRequestCommand(
    Guid RequestId)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.ApprovalRequest, RequestId);
}

public class RestoreApprovalRequestCommandHandler : IRequestHandler<RestoreApprovalRequestCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RestoreApprovalRequestCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RestoreApprovalRequestCommand request, CancellationToken ct)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (approvalRequest is null) throw new NotFoundException("ApprovalRequest", request.RequestId);

        approvalRequest.Restore(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
