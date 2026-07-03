using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;

public record CreateAutomationRuleCommand(
    Guid WorkspaceId,
    string Name,
    string TriggerEvent,
    string ActionType,
    string Configuration) : ICommand<Result<Guid>>, ITransactionalRequest;

public class CreateAutomationRuleCommandHandler : IRequestHandler<CreateAutomationRuleCommand, Result<Guid>>
{
    private readonly IAutomationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspacePermissionService _permissions;
    private readonly ICurrentTenantContext _tenant;

    public CreateAutomationRuleCommandHandler(
        IAutomationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IWorkspacePermissionService permissions,
        ICurrentTenantContext tenant)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _permissions = permissions;
        _tenant = tenant;
    }

    public async Task<Result<Guid>> Handle(CreateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        await _permissions.EnsureCanManageWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, cancellationToken);

        var trigger = AutomationTriggerDefinition.Create(request.TriggerEvent, request.Configuration);
        var action = AutomationActionDefinition.Create(request.ActionType, request.Configuration);
        var config = AutomationConfiguration.Create(trigger, action);

        var rule = AutomationRule.Create(
            _tenant.RequireAccountId(),
            request.WorkspaceId,
            request.Name,
            config,
            _currentUser.UserId,
            _dateTimeProvider.UtcNow);

        _context.AutomationRules.Add(rule);

        return Result<Guid>.Success(rule.Id);
    }
}
