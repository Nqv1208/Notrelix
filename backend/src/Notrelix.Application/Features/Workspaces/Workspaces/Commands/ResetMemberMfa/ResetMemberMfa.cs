using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Public.Access;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Workspaces.Workspaces.Commands.ResetMemberMfa;

/// <summary>
/// The Owner resets another member's second factor. Only the Owner may; an Admin gets the identical NotFound.
/// The proof is a fresh ResetMemberMfa step-up bound to this workspace and this member, spent only once the
/// caller is proven Owner and the target is a resettable member, so a refusal never spends it for nothing.
/// Nobody can reset an Owner's factors in the product. A second factor belongs to the person across Accounts, so
/// a Guest, or a person who is also active in another Account, is out of reach (else anyone could invite a victim
/// to their own workspace and strip the victim's factor everywhere).
/// </summary>
public record ResetMemberMfaCommand(Guid WorkspaceId, Guid MemberId, string StepUpToken)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ResetMemberMfaCommandHandler : IRequestHandler<ResetMemberMfaCommand, Result>
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly ISecurityStepUpService _stepUp;
    private readonly IIdentityUserAccess _userAccess;
    private readonly IMemberOtherAccountFacts _otherAccounts;

    public ResetMemberMfaCommandHandler(
        IWorkspaceDbContext context, ICurrentRequestContext requestContext,
        ISecurityStepUpService stepUp, IIdentityUserAccess userAccess, IMemberOtherAccountFacts otherAccounts)
    {
        _otherAccounts = otherAccounts;
        _context = context;
        _requestContext = requestContext;
        _stepUp = stepUp;
        _userAccess = userAccess;
    }

    public async Task<Result> Handle(ResetMemberMfaCommand request, CancellationToken ct)
    {
        var callerId = _requestContext.UserId;

        var caller = await _context.WorkspaceMembers.FirstOrDefaultAsync(
            m => m.WorkspaceId == request.WorkspaceId && m.UserId == callerId
                && m.Role == WorkspaceRole.Owner && m.Status == WorkspaceMemberStatus.Active, ct);
        if (caller is null)
            throw new NotFoundException(nameof(WorkspaceMember), callerId);

        var member = await _context.WorkspaceMembers.FirstOrDefaultAsync(
            m => m.WorkspaceId == request.WorkspaceId && m.UserId == request.MemberId
                && m.Status == WorkspaceMemberStatus.Active, ct);
        if (member is null)
            throw new NotFoundException(nameof(WorkspaceMember), request.MemberId);

        if (member.Role == WorkspaceRole.Guest)
            throw new BusinessRuleException("A Guest's second factor cannot be reset by a workspace Owner.");

        // An Owner of any workspace (and the caller) is out of reach: their own recovery code is their path.
        var targetIsOwner = request.MemberId == callerId || await _context.WorkspaceMembers.AnyAsync(
            m => m.UserId == request.MemberId && m.Role == WorkspaceRole.Owner
                && m.Status == WorkspaceMemberStatus.Active, ct);
        if (targetIsOwner)
            throw new BusinessRuleException("An Owner's second factor cannot be reset in the product.");

        if (await _otherAccounts.IsActiveInAnotherAccountAsync(caller.AccountId, request.WorkspaceId, request.MemberId, ct))
            throw new BusinessRuleException("This person is also a member of another Account, so their second factor cannot be reset here.");

        if (string.IsNullOrWhiteSpace(request.StepUpToken) || _requestContext.SessionId is not { } sessionId)
        {
            return Result.Failure(new ApplicationError(
                "identity.security.step-up-required",
                "Strong verification is required for this action.",
                ApplicationErrorType.PreconditionFailed));
        }

        var proof = await _stepUp.ConsumeBoundAsync(
            request.StepUpToken, callerId, sessionId, StepUpPurpose.ResetMemberMfa,
            StepUpTarget.MemberMfaReset(request.WorkspaceId, request.MemberId), ct);
        if (!proof.Succeeded)
            return proof;

        if (!await _userAccess.ResetMfaAsync(request.MemberId, callerId, ct))
            throw new NotFoundException(nameof(WorkspaceMember), request.MemberId);

        return Result.Success();
    }
}
