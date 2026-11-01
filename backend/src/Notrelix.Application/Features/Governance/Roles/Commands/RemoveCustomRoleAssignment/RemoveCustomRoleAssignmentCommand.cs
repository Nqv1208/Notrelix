using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Features.Governance.Roles.Commands.RemoveCustomRoleAssignment;

[IdempotencyOperation("governance.roles.remove-custom-role-assignment.v1")]
public sealed record RemoveCustomRoleAssignmentCommand(Guid WorkspaceId, Guid UserId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChangeMemberRole;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef? IRequirePermission.Resource => Resource;
}

public sealed class RemoveCustomRoleAssignmentCommandValidator : AbstractValidator<RemoveCustomRoleAssignmentCommand>
{
    public RemoveCustomRoleAssignmentCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

/// <summary>
/// Releases a person from their custom role; they return to the built-in role Workspaces stores. Allowed on any
/// plan (a frozen role must stay removable). Removing a role the person does not hold is a no-op.
/// </summary>
public sealed class RemoveCustomRoleAssignmentCommandHandler : IRequestHandler<RemoveCustomRoleAssignmentCommand, Result>
{
    private readonly IGovernanceDbContext _context;
    private readonly CustomRoleChangeRecorder _recorder;
    private readonly IAccessChangeSignal _access;

    public RemoveCustomRoleAssignmentCommandHandler(IGovernanceDbContext context, CustomRoleChangeRecorder recorder, IAccessChangeSignal access)
    {
        _context = context;
        _recorder = recorder;
        _access = access;
    }

    public async Task<Result> Handle(RemoveCustomRoleAssignmentCommand command, CancellationToken cancellationToken)
    {
        var current = await _context.MemberRoleAssignments
            .FirstOrDefaultAsync(a => a.WorkspaceId == command.WorkspaceId && a.MemberId == command.UserId, cancellationToken);
        if (current is null)
        {
            return Result.Success();
        }

        var role = await _context.CustomRoles.FirstOrDefaultAsync(r => r.Id == current.CustomRoleId, cancellationToken)
            ?? throw new NotFoundException("CustomRole", current.CustomRoleId);
        _context.MemberRoleAssignments.Remove(current);
        await _recorder.RecordAsync("Removed", role, command.WorkspaceId, command.UserId, "back to the built-in role", cancellationToken);
        _access.WorkspacePerson(command.WorkspaceId, command.UserId);
        return Result.Success();
    }
}
