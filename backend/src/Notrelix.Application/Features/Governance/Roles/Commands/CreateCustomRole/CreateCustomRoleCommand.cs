using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Domain.Governance;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Governance.Roles.Commands.CreateCustomRole;

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
