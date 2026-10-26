using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.RulesEngine;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Features.Automation.Rules.Services;

/// <summary>One action of a rule as the caller sends it.</summary>
public sealed record RuleActionInput(string Type, string? Configuration);

/// <summary>
/// Builds a rule's configuration from what the author sent: one trigger, optional AND conditions
/// and up to five ordered actions. Only actions with an executor are accepted, and a board rule
/// takes no integration action (those need the integrations permission).
/// </summary>
public static class AutomationRuleAssembler
{
    public static AutomationConfiguration Assemble(
        string triggerEvent,
        string? triggerConfiguration,
        Guid? boardId,
        IReadOnlyList<RuleActionInput> actions,
        string? conditions)
    {
        if (actions.Count == 0)
            throw Invalid("A rule needs at least one action.");

        // A rule without a board keeps the single configuration both its trigger and action read.
        var trigger = AutomationTriggerDefinition.Create(
            triggerEvent, boardId is null ? actions[0].Configuration : triggerConfiguration);

        var definitions = new List<AutomationActionDefinition>();
        foreach (var input in actions)
        {
            var definition = AutomationActionDefinition.Create(input.Type, input.Configuration);
            if (!AutomationActionConfigs.HasExecutor(definition.Type))
                throw Invalid($"The action '{definition.Type}' cannot run yet.");
            if (boardId is not null && AutomationActionKinds.IsIntegration(definition.Type))
                throw Invalid("A board rule cannot use an integration action.");
            if (boardId is null && definition.Type is not (AutomationActionConfigs.Types.MoveItem or AutomationActionConfigs.Types.Webhook))
                throw Invalid($"The action '{definition.Type}' needs a board rule.");
            if (definition.Type == AutomationActionConfigs.Types.Webhook)
                AutomationActionValidator.Validate(definition);
            definitions.Add(definition);
        }

        AutomationConditionDefinition? condition = null;
        if (!string.IsNullOrWhiteSpace(conditions))
        {
            if (boardId is null || !AutomationConditions.TryParse(conditions, out _))
                throw Invalid("Conditions need a board rule and must be well formed.");
            condition = AutomationConditionDefinition.Create(conditions);
        }

        return AutomationConfiguration.Create(trigger, definitions[0], condition, definitions.Skip(1));
    }

    private static DomainBusinessRuleException Invalid(string message) =>
        new(AutomationRuleCodes.Automation_Action_InvalidConfigJson, message);
}
