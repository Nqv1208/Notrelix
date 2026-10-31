using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Billing.Public.Seats;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Governance.Roles;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Governance.Roles.Commands;

// Role definitions are Account-scoped: whoever manages the Account (Owner, Admin) writes them (owner
// decision 2026-10-31). Assigning one to a person is a workspace act of the workspace Owner.

[IdempotencyOperation("governance.roles.create-custom-role.v1")]
public sealed record CreateCustomRoleCommand(
    string Name,
    string? Description,
    string BaseRole,
    IReadOnlyList<string> AddedActions,
    IReadOnlyList<string> RemovedActions)
    : ICommand<Result<CustomRoleDto>>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageAccount;
    public ResourceRef? Resource => null;
}

public sealed class CreateCustomRoleCommandValidator : AbstractValidator<CreateCustomRoleCommand>
{
    public CreateCustomRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(CustomRole.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(CustomRole.DescriptionMaxLength);
        RuleFor(x => x.BaseRole).NotEmpty();
        RuleFor(x => x.AddedActions).NotNull();
        RuleFor(x => x.RemovedActions).NotNull();
    }
}

public sealed class CreateCustomRoleCommandHandler : IRequestHandler<CreateCustomRoleCommand, Result<CustomRoleDto>>
{
    public const int MaxRolesPerAccount = 100;

    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _request;
    private readonly IDateTimeProvider _clock;
    private readonly IBillingCapabilityFacts _billing;
    private readonly CustomRoleChangeRecorder _recorder;

    public CreateCustomRoleCommandHandler(
        IGovernanceDbContext context, ICurrentRequestContext request, IDateTimeProvider clock,
        IBillingCapabilityFacts billing, CustomRoleChangeRecorder recorder)
    {
        _context = context;
        _request = request;
        _clock = clock;
        _billing = billing;
        _recorder = recorder;
    }

    public async Task<Result<CustomRoleDto>> Handle(CreateCustomRoleCommand command, CancellationToken cancellationToken)
    {
        var accountId = _request.RequireAccountId();
        await CustomRoleRules.EnsurePlanAsync(_billing, accountId, cancellationToken);

        var baseRole = CustomRoleRules.ParseBase(command.BaseRole);
        var added = command.AddedActions.Select(a => a.Trim()).Distinct().ToArray();
        var removed = command.RemovedActions.Select(a => a.Trim()).Distinct().ToArray();
        CustomRoleRules.ValidateActions(baseRole, added, removed);

        var key = command.Name.Trim().ToLower();
        if (await _context.CustomRoles.AnyAsync(r => r.AccountId == accountId && r.Name.ToLower() == key, cancellationToken))
        {
            throw new ConflictException("A role with this name already exists.", "governance.role.name_taken");
        }

        if (await _context.CustomRoles.CountAsync(r => r.AccountId == accountId, cancellationToken) >= MaxRolesPerAccount)
        {
            throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_LimitReached, $"An account has at most {MaxRolesPerAccount} custom roles.");
        }

        var role = CustomRole.Create(accountId, command.Name, command.Description, baseRole, added, removed, _request.UserId, _clock.UtcNow);
        _context.CustomRoles.Add(role);
        await _recorder.RecordAsync("Created", role, null, null, $"+{added.Length} -{removed.Length} on {baseRole}", cancellationToken);
        return Result<CustomRoleDto>.Success(CustomRoleRules.ToDto(role, 0));
    }
}

[IdempotencyOperation("governance.roles.update-custom-role.v1")]
public sealed record UpdateCustomRoleCommand(
    Guid RoleId,
    long ExpectedVersion,
    string Name,
    string? Description,
    IReadOnlyList<string> AddedActions,
    IReadOnlyList<string> RemovedActions)
    : ICommand<Result<CustomRoleDto>>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageAccount;
    public ResourceRef? Resource => null;
}

public sealed class UpdateCustomRoleCommandValidator : AbstractValidator<UpdateCustomRoleCommand>
{
    public UpdateCustomRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(CustomRole.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(CustomRole.DescriptionMaxLength);
        RuleFor(x => x.AddedActions).NotNull();
        RuleFor(x => x.RemovedActions).NotNull();
    }
}

public sealed class UpdateCustomRoleCommandHandler : IRequestHandler<UpdateCustomRoleCommand, Result<CustomRoleDto>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _request;
    private readonly IDateTimeProvider _clock;
    private readonly IBillingCapabilityFacts _billing;
    private readonly CustomRoleChangeRecorder _recorder;
    private readonly IAccessChangeSignal _access;

    public UpdateCustomRoleCommandHandler(
        IGovernanceDbContext context, ICurrentRequestContext request, IDateTimeProvider clock,
        IBillingCapabilityFacts billing, CustomRoleChangeRecorder recorder, IAccessChangeSignal access)
    {
        _context = context;
        _request = request;
        _clock = clock;
        _billing = billing;
        _recorder = recorder;
        _access = access;
    }

    public async Task<Result<CustomRoleDto>> Handle(UpdateCustomRoleCommand command, CancellationToken cancellationToken)
    {
        var accountId = _request.RequireAccountId();
        await CustomRoleRules.EnsurePlanAsync(_billing, accountId, cancellationToken);

        var role = await _context.CustomRoles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == command.RoleId && r.AccountId == accountId, cancellationToken)
            ?? throw new NotFoundException("CustomRole", command.RoleId);
        CustomRoleRules.EnsureVersion(role, command.ExpectedVersion);

        var added = command.AddedActions.Select(a => a.Trim()).Distinct().ToArray();
        var removed = command.RemovedActions.Select(a => a.Trim()).Distinct().ToArray();
        CustomRoleRules.ValidateActions(role.BaseRole, added, removed);

        var key = command.Name.Trim().ToLower();
        if (await _context.CustomRoles.AnyAsync(r => r.AccountId == accountId && r.Id != role.Id && r.Name.ToLower() == key, cancellationToken))
        {
            throw new ConflictException("A role with this name already exists.", "governance.role.name_taken");
        }

        var changed = role.Revise(command.Name, command.Description, added, removed, _request.UserId, _clock.UtcNow);
        var holders = await _context.MemberRoleAssignments.IgnoreQueryFilters().Where(a => a.AccountId == accountId && a.CustomRoleId == role.Id).ToListAsync(cancellationToken);
        if (changed)
        {
            await _recorder.RecordAsync("Updated", role, null, null, $"+{added.Length} -{removed.Length}, {holders.Count} holders", cancellationToken);
            // The new action set applies at once: every workspace holding the role re-authorizes.
            foreach (var workspaceId in holders.Select(h => h.WorkspaceId).Distinct())
            {
                _access.Workspace(workspaceId);
            }
        }

        return Result<CustomRoleDto>.Success(CustomRoleRules.ToDto(role, holders.Count));
    }
}

[IdempotencyOperation("governance.roles.archive-custom-role.v1")]
public sealed record ArchiveCustomRoleCommand(Guid RoleId, long ExpectedVersion, Guid? MoveToRoleId)
    : ICommand<Result<CustomRoleDto>>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageAccount;
    public ResourceRef? Resource => null;
}

public sealed class ArchiveCustomRoleCommandValidator : AbstractValidator<ArchiveCustomRoleCommand>
{
    public ArchiveCustomRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}

/// <summary>
/// Archives a role. Its holders move to a chosen role of the account (never one with a higher base, so
/// nobody is lifted to Admin); a role with no holder needs no target. Archiving is allowed on any plan:
/// it only reduces what the role grants.
/// </summary>
public sealed class ArchiveCustomRoleCommandHandler : IRequestHandler<ArchiveCustomRoleCommand, Result<CustomRoleDto>>
{
    private readonly IGovernanceDbContext _context;
    private readonly ICurrentRequestContext _request;
    private readonly IDateTimeProvider _clock;
    private readonly CustomRoleChangeRecorder _recorder;
    private readonly IAccessChangeSignal _access;

    public ArchiveCustomRoleCommandHandler(
        IGovernanceDbContext context, ICurrentRequestContext request, IDateTimeProvider clock,
        CustomRoleChangeRecorder recorder, IAccessChangeSignal access)
    {
        _context = context;
        _request = request;
        _clock = clock;
        _recorder = recorder;
        _access = access;
    }

    public async Task<Result<CustomRoleDto>> Handle(ArchiveCustomRoleCommand command, CancellationToken cancellationToken)
    {
        var accountId = _request.RequireAccountId();
        var role = await _context.CustomRoles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == command.RoleId && r.AccountId == accountId, cancellationToken)
            ?? throw new NotFoundException("CustomRole", command.RoleId);

        // Archiving twice is a no-op: nothing moves, no version bumps, no event.
        if (role.Status == CustomRoleStatus.Archived)
        {
            return Result<CustomRoleDto>.Success(CustomRoleRules.ToDto(role, 0));
        }

        CustomRoleRules.EnsureVersion(role, command.ExpectedVersion);

        var holders = await _context.MemberRoleAssignments.IgnoreQueryFilters().Where(a => a.AccountId == accountId && a.CustomRoleId == role.Id).ToListAsync(cancellationToken);
        if (holders.Count > 0)
        {
            if (command.MoveToRoleId is not { } targetId)
            {
                throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_ArchiveNeedsTarget, $"{holders.Count} people hold this role; choose the role they move to.");
            }

            var target = await _context.CustomRoles
                .FirstOrDefaultAsync(r => r.Id == targetId && r.AccountId == accountId, cancellationToken)
                ?? throw new NotFoundException("CustomRole", targetId);
            if (target.Id == role.Id || target.Status != CustomRoleStatus.Active
                || CustomRoleRank.Of(target.BaseRole) > CustomRoleRank.Of(role.BaseRole))
            {
                throw new DomainBusinessRuleException(GovernanceRuleCodes.Governance_Role_ArchiveTargetInvalid, "People can only move to another active role that is not above this one.");
            }

            foreach (var holder in holders)
            {
                holder.MoveTo(target.Id, _clock.UtcNow);
            }
        }

        role.Archive(_request.UserId, _clock.UtcNow);
        await _recorder.RecordAsync("Archived", role, null, null, $"{holders.Count} holders moved to {command.MoveToRoleId}", cancellationToken);
        foreach (var workspaceId in holders.Select(h => h.WorkspaceId).Distinct())
        {
            _access.Workspace(workspaceId);
        }

        return Result<CustomRoleDto>.Success(CustomRoleRules.ToDto(role, 0));
    }
}

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
