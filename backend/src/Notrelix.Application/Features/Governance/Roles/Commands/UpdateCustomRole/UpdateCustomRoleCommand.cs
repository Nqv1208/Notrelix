using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Governance.Abstractions;

namespace Notrelix.Application.Features.Governance.Roles.Commands.UpdateCustomRole;

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
