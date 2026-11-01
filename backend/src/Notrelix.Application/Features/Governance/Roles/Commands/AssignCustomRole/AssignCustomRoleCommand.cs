using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Billing.Public.Seats;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Governance;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Governance.Roles.Commands.AssignCustomRole;

[IdempotencyOperation("governance.roles.assign-custom-role.v1")]
public sealed record AssignCustomRoleCommand(Guid WorkspaceId, Guid UserId, Guid RoleId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ChangeMemberRole;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    ResourceRef? IRequirePermission.Resource => Resource;
}

public sealed class AssignCustomRoleCommandValidator : AbstractValidator<AssignCustomRoleCommand>
{
    public AssignCustomRoleCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public sealed class AssignCustomRoleCommandHandler : IRequestHandler<AssignCustomRoleCommand, Result>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _request;
    private readonly IDateTimeProvider _clock;
    private readonly IBillingCapabilityFacts _billing;
    private readonly IBillingSeatGate _seats;
    private readonly IWorkspaceMemberFacts _members;
    private readonly CustomRoleChangeRecorder _recorder;
    private readonly IAccessChangeSignal _access;

    public AssignCustomRoleCommandHandler(
        IGovernanceDbContext context, ICurrentRequestContext request, IDateTimeProvider clock,
        IBillingCapabilityFacts billing, IBillingSeatGate seats, IWorkspaceMemberFacts members,
        CustomRoleChangeRecorder recorder, IAccessChangeSignal access)
    {
        _context = context;
        _request = request;
        _clock = clock;
        _billing = billing;
        _seats = seats;
        _members = members;
        _recorder = recorder;
        _access = access;
    }

    public async Task<Result> Handle(AssignCustomRoleCommand command, CancellationToken cancellationToken)
    {
        var accountId = _request.RequireAccountId();
        await CustomRoleRules.EnsurePlanAsync(_billing, accountId, cancellationToken);

        // A role of another account is as missing as one that does not exist.
        var role = await _context.CustomRoles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == command.RoleId && r.AccountId == accountId, cancellationToken)
            ?? throw new NotFoundException("CustomRole", command.RoleId);
        if (role.Status != CustomRoleStatus.Active)
        {
            throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_Archived, "An archived role cannot be assigned.");
        }

        var member = await _members.FindAsync(command.WorkspaceId, command.UserId, cancellationToken);
        if (member is not { IsActive: true })
        {
            throw new NotFoundException("WorkspaceMember", command.UserId);
        }

        // The Owner is never replaced by a role.
        if (member.Role == "Owner")
        {
            throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_OwnerCannotHoldRole, "A workspace Owner keeps the Owner role.");
        }

        var current = await _context.MemberRoleAssignments
            .FirstOrDefaultAsync(a => a.WorkspaceId == command.WorkspaceId && a.MemberId == command.UserId, cancellationToken);
        if (current?.CustomRoleId == role.Id)
        {
            return Result.Success();
        }

        // A Guest given a Member-or-higher base takes a member seat.
        if (member.Role == "Guest" && role.BaseRole != CustomRoleBase.Guest
            && !await _seats.CanAdmitAsync(accountId, command.WorkspaceId, command.UserId, asGuest: false, cancellationToken))
        {
            throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_NoSeat, "No member seat is free for this role.");
        }

        if (current is null)
        {
            _context.MemberRoleAssignments.Add(
                MemberRoleAssignment.Create(accountId, command.WorkspaceId, command.UserId, role.Id, _clock.UtcNow));
        }
        else
        {
            current.MoveTo(role.Id, _clock.UtcNow);
        }

        await _recorder.RecordAsync("Assigned", role, command.WorkspaceId, command.UserId, $"base {role.BaseRole}", cancellationToken);
        _access.WorkspacePerson(command.WorkspaceId, command.UserId);
        return Result.Success();
    }
}
