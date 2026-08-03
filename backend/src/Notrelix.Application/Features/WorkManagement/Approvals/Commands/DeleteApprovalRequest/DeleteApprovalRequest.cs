using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.DeleteApprovalRequest;

[IdempotencyOperation("work-management.approvals.delete-approval-request.v1")]
public record DeleteApprovalRequestCommand(
    Guid RequestId,
    long ExpectedVersion)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.ApprovalRequest, RequestId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class DeleteApprovalRequestCommandHandler : IRequestHandler<DeleteApprovalRequestCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteApprovalRequestCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteApprovalRequestCommand request, CancellationToken ct)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (approvalRequest is null) throw new NotFoundException("ApprovalRequest", request.RequestId);

        approvalRequest.Delete(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
