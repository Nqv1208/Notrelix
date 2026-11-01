using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Public.DefaultWorkspace;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;

/// <summary>
/// The caller ends their own participation in a Workspace. It is the membership transition of
/// <c>RemoveMember</c> with the caller as both subject and actor: any active member may leave
/// (<c>ViewWorkspace</c> on it), the last active Owner may not (the Workspace would be left without
/// an owner), the workspace access grant is revoked in the same transaction, and a default Workspace
/// choice pointing at it is cleared. Leaving is not Identity deletion and does not touch work assigned
/// to the person.
/// </summary>
public record LeaveWorkspaceCommand(Guid WorkspaceId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class LeaveWorkspaceCommandValidator : AbstractValidator<LeaveWorkspaceCommand>
{
    public LeaveWorkspaceCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
    }
}

public class LeaveWorkspaceCommandHandler : IRequestHandler<LeaveWorkspaceCommand, Result>
{
    public const string LastOwnerErrorCode = "workspaces.member.last-owner-cannot-leave";

    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspaceGrantProjectionService _grantProjection;
    private readonly IWorkspaceOwnerUpdateLocker _locker;
    private readonly IIdentityDefaultWorkspace _defaultWorkspace;

    private readonly IAccessChangeSignal _access;

    public LeaveWorkspaceCommandHandler(
        IWorkspaceDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IWorkspaceGrantProjectionService grantProjection,
        IWorkspaceOwnerUpdateLocker locker,
        IIdentityDefaultWorkspace defaultWorkspace,
        IAccessChangeSignal access)
    {
        _access = access;
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _grantProjection = grantProjection;
        _locker = locker;
        _defaultWorkspace = defaultWorkspace;
    }

    public async Task<Result> Handle(LeaveWorkspaceCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;

        // Serializes owner-affecting changes so the active-owner count below cannot be stale.
        var workspace = await _locker.LockWorkspaceAsync(request.WorkspaceId, ct)
            ?? throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspace.Id
                && m.UserId == userId
                && m.Status == WorkspaceMemberStatus.Active, ct)
            ?? throw new NotFoundException(nameof(WorkspaceMember), userId);

        var activeOwnerCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspace.Id
                && m.Role == WorkspaceRole.Owner
                && m.Status == WorkspaceMemberStatus.Active, ct);

        try
        {
            member.Remove(activeOwnerCount, userId, _dateTimeProvider.UtcNow);
        }
        catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException ex)
            when (ex.RuleCode == WorkspaceRuleCodes.Workspaces_Owner_CannotRemoveLastOwner)
        {
            // The Domain rejected before changing anything; report it as a conflict with a stable code.
            throw new ConflictException(
                "The last owner cannot leave; hand the workspace over first.", LastOwnerErrorCode);
        }

        await _grantProjection.RevokeLeavingMemberGrantAsync(
            workspace.AccountId,
            workspace.Id,
            userId,
            _dateTimeProvider.UtcNow,
            ct);

        // Derived access ends with the membership, in the same transaction.
        await _grantProjection.RevokeAccountGrantWhenLastWorkspaceLeftAsync(
            workspace.AccountId, workspace.Id, userId, _dateTimeProvider.UtcNow, ct);
        await _grantProjection.ClearCustomRoleAssignmentsAsync(
            workspace.AccountId, workspace.Id, userId, ct);

        await _defaultWorkspace.ClearIfAsync(userId, workspace.Id, ct);

        _access.WorkspacePerson(request.WorkspaceId, _requestContext.UserId);
        return Result.Success();
    }
}
