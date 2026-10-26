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
        CancellationToken cancellationToken,
        Guid? boardId = null)
    {
        var principal = new AutomationPrincipal(accountId, executorUserId, workspaceId);
        var isMoveItem = string.Equals(
            action.Type, AutomationMoveItemConfiguration.ActionType, StringComparison.OrdinalIgnoreCase);

        // AU-2: a board-scoped rule's board must be in the workspace and editable by its
        // executor whatever the action is; nothing about the board is trusted from the request.
        if (boardId is { } board)
        {
            Guid? targetGroup = null;
            switch (action.Type)
            {
                case AutomationActionConfigs.Types.MoveItem:
                    // AU-1: the board-scoped shape moves the triggering item; a fixed item is gone.
                    if (AutomationMoveItemConfiguration.TryParse(action.Configuration, out _, out _)
                        || !AutomationMoveItemConfiguration.TryParseTarget(action.Configuration, out var group))
                    {
                        throw new DomainBusinessRuleException(
                            AutomationRuleCodes.Automation_ActionValidator_InvalidMoveItemJson,
                            "A board rule's MoveItem action takes a target group only, not a fixed item.");
                    }

                    targetGroup = group;
                    break;
                case AutomationActionConfigs.Types.ChangeStatus:
                    await _workActions.EnsureBoardGroupAsync(board, null, principal, cancellationToken);
                    if (!AutomationActionConfigs.TryParseChangeStatus(action.Configuration, out var optionId))
                        throw Invalid("ChangeStatus needs a status option.");
                    await _workActions.EnsureStatusOptionAsync(board, optionId, principal, cancellationToken);
                    return;
                case AutomationActionConfigs.Types.UpdateField:
                    await _workActions.EnsureBoardGroupAsync(board, null, principal, cancellationToken);
                    if (!AutomationActionConfigs.TryParseUpdateField(action.Configuration, out var fieldId, out var valueJson))
                        throw Invalid("UpdateField needs a field and a value.");
                    await _workActions.EnsureFieldValueAsync(board, fieldId, valueJson, principal, cancellationToken);
                    return;
                case AutomationActionConfigs.Types.AssignPerson:
                    await _workActions.EnsureBoardGroupAsync(board, null, principal, cancellationToken);
                    if (!AutomationActionConfigs.TryParseAssign(action.Configuration, out var assignee))
                        throw Invalid("AssignPerson needs a member.");
                    await _workActions.EnsureWorkspaceMemberAsync(assignee, principal, cancellationToken);
                    return;
                case AutomationActionConfigs.Types.NotifyMember:
                    await _workActions.EnsureBoardGroupAsync(board, null, principal, cancellationToken);
                    if (!AutomationActionConfigs.TryParseNotify(action.Configuration, out var notified, out _))
                        throw Invalid("NotifyMember needs a member.");
                    await _workActions.EnsureWorkspaceMemberAsync(notified, principal, cancellationToken);
                    return;
                case AutomationActionConfigs.Types.SendEmail:
                    await _workActions.EnsureBoardGroupAsync(board, null, principal, cancellationToken);
                    if (!AutomationActionConfigs.TryParseEmail(action.Configuration, out var recipient, out _, out _))
                        throw Invalid("SendEmail needs a member and a subject.");
                    await _workActions.EnsureWorkspaceMemberAsync(recipient, principal, cancellationToken);
                    return;
                default:
                    throw Invalid($"The action '{action.Type}' is not available for a board rule.");
            }

            await _workActions.EnsureBoardGroupAsync(board, targetGroup, principal, cancellationToken);
            return;
        }

        if (!isMoveItem)
        {
            return;
        }

        if (!AutomationMoveItemConfiguration.TryParse(action.Configuration, out var itemId, out var targetGroupId))
        {
            throw new DomainBusinessRuleException(
                AutomationRuleCodes.Automation_ActionValidator_InvalidMoveItemJson,
                "MoveItem action configuration is invalid.");
        }

        await _workActions.EnsureMoveItemAllowedAsync(itemId, targetGroupId, principal, cancellationToken);
    }

    /// <summary>
    /// Validates the whole rule for its executor: every action, and every condition's targets
    /// (a board rule only; conditions read the triggering item, which a workspace rule has none of).
    /// </summary>
    public async Task EnsureRuleAsync(
        Guid accountId,
        Guid workspaceId,
        Guid executorUserId,
        AutomationConfiguration configuration,
        Guid? boardId,
        CancellationToken cancellationToken)
    {
        foreach (var action in configuration.Actions)
        {
            await EnsureCanExecuteAsync(accountId, workspaceId, executorUserId, action, cancellationToken, boardId);
        }

        if (configuration.Condition is null)
        {
            return;
        }

        if (boardId is not { } board
            || !AutomationConditions.TryParse(configuration.Condition.RawJson, out var conditions))
        {
            throw Invalid("Conditions are only available on a board rule and must be well formed.");
        }

        var principal = new AutomationPrincipal(accountId, executorUserId, workspaceId);
        foreach (var condition in conditions)
        {
            switch (condition.Kind)
            {
                case "group":
                    await _workActions.EnsureBoardGroupAsync(board, condition.GroupId, principal, cancellationToken);
                    break;
                case "status":
                    await _workActions.EnsureStatusOptionAsync(board, condition.OptionId!.Value, principal, cancellationToken);
                    break;
                case "field":
                    await _workActions.EnsureFieldValueAsync(board, condition.FieldId!.Value, condition.ValueJson!, principal, cancellationToken);
                    break;
            }
        }
    }

    private static DomainBusinessRuleException Invalid(string message) =>
        new(AutomationRuleCodes.Automation_Action_InvalidConfigJson, message);
}
