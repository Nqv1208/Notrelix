using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;

/// <summary>
/// Hands the workspace to another active, non-Guest member. The most sensitive account action: it needs a
/// single-use step-up proof for the TransferWorkspaceOwnership purpose, bound to this workspace and this
/// incoming Owner, spent only once the caller is
/// proven to be the active Owner and the target exists, so a non-Owner still gets the identical NotFound.
/// </summary>
public record TransferOwnershipCommand(
    Guid WorkspaceId,
    Guid NewOwnerId,
    long ExpectedVersion,
    string StepUpToken
) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => Resource;
}

public class TransferOwnershipCommandHandler : IRequestHandler<TransferOwnershipCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceGrantProjectionService _grantProjection;
    private readonly IWorkspaceOwnerUpdateLocker _locker;
    private readonly ISecurityStepUpService _stepUp;

    public TransferOwnershipCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceGrantProjectionService grantProjection,
        IWorkspaceOwnerUpdateLocker locker,
        ISecurityStepUpService stepUp)
    {
        _stepUp = stepUp;
        _locker = locker;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
    }

    public async Task<Result> Handle(TransferOwnershipCommand request, CancellationToken ct)
    {
        var currentOwnerId = _requestContext.UserId;

        if (currentOwnerId == request.NewOwnerId)
            throw new BusinessRuleException("Cannot transfer ownership to yourself.");

        // Serialize with Suspend/Remove/UpdateMemberRole on the workspace row so a
        // concurrent removal of the incoming owner cannot interleave with the transfer.
        var workspace = await _locker.LockWorkspaceAsync(request.WorkspaceId, ct);
        if (workspace is null)
            throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var currentOwner = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(
                m => m.WorkspaceId == request.WorkspaceId
                    && m.UserId == currentOwnerId
                    && m.Role == WorkspaceRole.Owner
                    && m.Status == WorkspaceMemberStatus.Active,
                ct);

        if (currentOwner is null)
            throw new NotFoundException(nameof(WorkspaceMember), currentOwnerId);

        var newOwner = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(
                m => m.WorkspaceId == request.WorkspaceId
                    && m.UserId == request.NewOwnerId
                    && m.Status == WorkspaceMemberStatus.Active,
                ct);

        if (newOwner is null)
            throw new NotFoundException(nameof(WorkspaceMember), request.NewOwnerId);

        var stepUp = await ConsumeStepUpAsync(currentOwnerId, request, ct);
        if (!stepUp.Succeeded)
            return stepUp;

        var now = _dateTimeProvider.UtcNow;

        newOwner.PromoteToOwner(currentOwnerId, now);
        currentOwner.ChangeRole(WorkspaceRole.Admin, currentOwnerId, 2, now);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            newOwner.AccountId,
            request.WorkspaceId,
            request.NewOwnerId,
            WorkspaceRole.Owner,
            now,
            ct);

        await _grantProjection.SyncWorkspaceMemberGrantAsync(
            currentOwner.AccountId,
            request.WorkspaceId,
            currentOwnerId,
            WorkspaceRole.Admin,
            now,
            ct);

        return Result.Success();
    }

    private async Task<Result> ConsumeStepUpAsync(Guid userId, TransferOwnershipCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.StepUpToken) || _requestContext.SessionId is not { } sessionId)
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        // The proof names this workspace and this incoming Owner: one given for another person is refused (and spent).
        return await _stepUp.ConsumeBoundAsync(
            request.StepUpToken, userId, sessionId, StepUpPurpose.TransferWorkspaceOwnership,
            StepUpTarget.OwnershipTransfer(request.WorkspaceId, request.NewOwnerId), ct);
    }
}
