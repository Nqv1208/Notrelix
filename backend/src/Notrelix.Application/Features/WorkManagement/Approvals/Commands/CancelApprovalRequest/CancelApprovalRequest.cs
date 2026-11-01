using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;

/// <summary>
/// Withdraws a pending approval request. The person who asked may cancel their own request while they can still
/// read the Board of the Item it is about; anyone else needs <c>ManageBoard</c> on the request. Any member reaches
/// the use case (<c>ViewWorkspace</c>); the requester-or-manager rule in the handler is the authority. Only a
/// pending request can be cancelled (Domain).
/// </summary>
[IdempotencyOperation("work-management.approvals.cancel-approval-request.v1")]
public record CancelApprovalRequestCommand(
    Guid RequestId,
    long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.approval-request"), RequestId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class CancelApprovalRequestCommandHandler : IRequestHandler<CancelApprovalRequestCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IResourceVisibility _visibility;
    private readonly IExecutionContextReader _executionContext;
    private readonly IAccessFactsProvider _facts;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IBoardRealtimeNotifier? _realtime;

    public CancelApprovalRequestCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IResourceVisibility visibility,
        IExecutionContextReader executionContext,
        IAccessFactsProvider facts,
        IAccessPolicyEvaluator policy,
        IBoardRealtimeNotifier? realtime = null)
    {
        _realtime = realtime;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _visibility = visibility;
        _executionContext = executionContext;
        _facts = facts;
        _policy = policy;
    }

    private sealed record ManageProbe(ResourceRef? Resource) : IRequirePermission
    {
        public PermissionAction Action => PermissionAction.ManageBoard;
    }

    private static readonly RequestDescriptor ManageProbeDescriptor = new(
        typeof(ManageProbe),
        ApplicationRequestKind.Command,
        ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource,
        ApplicationDataAccessKind.Write,
        new AccessRequirements(RequiresPermission: true, RequiresVerifiedEmail: false, RequiresSubscription: false, RequiresFeature: false),
        IsIdempotent: false,
        RequiresExpectedVersion: false);

    public async Task<Result> Handle(CancelApprovalRequestCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (_realtime is not null) await _realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(CancelApprovalRequestCommand request, CancellationToken ct)
    {
        var approvalRequest = await _context.ApprovalRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, ct);
        if (approvalRequest is null) throw new NotFoundException("ApprovalRequest", request.RequestId);

        var userId = _requestContext.UserId;
        if (!await IsRequesterWhoCanStillReadAsync(approvalRequest, userId, ct) && !await CanManageAsync(request, ct))
            throw new ForbiddenException("Only the person who asked, or someone who manages the board, can cancel this approval.");

        approvalRequest.Cancel(userId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }

    private async Task<bool> IsRequesterWhoCanStillReadAsync(ApprovalRequest approval, Guid userId, CancellationToken ct)
    {
        if (approval.RequestedByUserId != userId) return false;
        if (approval.Target.Kind.Value != "work-management.board-item") return true;

        var boardId = await _context.BoardItems.AsNoTracking()
            .Where(i => i.Id == approval.Target.ResourceId && i.DeletedAt == null)
            .Select(i => (Guid?)i.BoardId)
            .FirstOrDefaultAsync(ct);
        return boardId is { } board
            && (await _visibility.VisibleBoardIdsAsync(approval.WorkspaceId, userId, ct)).Contains(board);
    }

    // The same decision the pipeline gave this command before the requester rule: ManageBoard on the request.
    private async Task<bool> CanManageAsync(CancelApprovalRequestCommand request, CancellationToken ct)
    {
        var context = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Execution context is not resolved for cancelling an approval request.");
        var probe = new ManageProbe(request.Resource);
        var facts = await _facts.ResolveAsync(ManageProbeDescriptor, context, probe, ct);
        return _policy.Evaluate(ManageProbeDescriptor, context, facts, probe).Kind == AccessDecisionKind.Allowed;
    }
}
