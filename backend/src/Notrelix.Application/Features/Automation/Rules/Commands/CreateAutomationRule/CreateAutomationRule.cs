using MediatR;
using Notrelix.Application.Common.Models;
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
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IWorkspacePermissionService _permissions;

    public CreateAutomationRuleCommandHandler(
        IApplicationDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IWorkspacePermissionService permissions)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _permissions = permissions;
    }

    public async Task<Result<Guid>> Handle(CreateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        await _permissions.EnsureCanManageWorkspaceAsync(request.WorkspaceId, _currentUser.UserId, cancellationToken);

        var trigger = AutomationTriggerDefinition.Create(request.TriggerEvent, request.Configuration);
        var action = AutomationActionDefinition.Create(request.ActionType, request.Configuration);
        var config = AutomationConfiguration.Create(trigger, action);

        var rule = AutomationRule.Create(
            request.WorkspaceId,
            request.Name,
            config,
            _currentUser.UserId,
            _dateTimeProvider.UtcNow);

        _context.AutomationRules.Add(rule);

        return Result<Guid>.Success(rule.Id);
    }
}
