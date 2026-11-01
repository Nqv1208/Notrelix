using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Domain.Governance;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Governance.Roles.Commands.ArchiveCustomRole;

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
