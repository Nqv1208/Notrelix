using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.RulesEngine;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Automation.Rules.Services;

/// <summary>
/// AUT-020: proves that the user a rule runs as holds the action's permission on
/// the action's target before the rule is created or made executable. The
/// target context owns the decision (the canonical MoveItem policy); this
/// service only supplies the executor and the configured target through the
/// Automation→Work port. Webhook rules are governed by ManageIntegrations at the
/// pipeline and have no Work target here.
/// </summary>
public sealed class AutomationExecutorAuthority
{
    private readonly IWorkActionPort _workActions;

    public AutomationExecutorAuthority(IWorkActionPort workActions)
    {
        _workActions = workActions;
    }

    public async Task EnsureCanExecuteAsync(
        Guid accountId,
        Guid workspaceId,
        Guid executorUserId,
        AutomationActionDefinition action,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(action.Type, AutomationMoveItemConfiguration.ActionType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!AutomationMoveItemConfiguration.TryParse(action.Configuration, out var itemId, out var targetGroupId))
        {
            throw new DomainBusinessRuleException(
                AutomationRuleCodes.Automation_ActionValidator_InvalidMoveItemJson,
                "MoveItem action configuration is invalid.");
        }

        await _workActions.EnsureMoveItemAllowedAsync(
            itemId,
            targetGroupId,
            new AutomationPrincipal(accountId, executorUserId, workspaceId),
            cancellationToken);
    }
}
