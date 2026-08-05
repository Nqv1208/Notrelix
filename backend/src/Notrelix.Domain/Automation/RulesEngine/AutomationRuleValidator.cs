namespace Notrelix.Domain.Automation.RulesEngine;

public static class AutomationRuleValidator
{
    /// <summary>
    /// Validates prospective state for activation.
    /// Used by Enable() and UpdateConfiguration() when rule is Active.
    /// </summary>
    public static void ValidateForActivation(
        string name,
        AutomationConfiguration? configuration)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException(
                AutomationRuleCodes.Automation_Rule_NameCannotBeEmpty,
                "Automation rule name cannot be empty.");

        if (configuration is null)
            throw new BusinessRuleException(
                AutomationRuleCodes.Automation_Rule_MustHaveConfiguration,
                "Automation rule must have a configuration.");

        if (configuration.Trigger is null)
            throw new BusinessRuleException(
                AutomationRuleCodes.Automation_Rule_MustHaveTrigger,
                "Automation rule must have a trigger.");

        if (configuration.Action is null)
            throw new BusinessRuleException(
                AutomationRuleCodes.Automation_Rule_MustHaveAction,
                "Automation rule must have an action.");

        AutomationTriggerValidator.Validate(configuration.Trigger);
        AutomationActionValidator.Validate(configuration.Action);
    }

    /// <summary>
    /// Validates current aggregate state.
    /// Wrapper for backward compatibility with existing callers.
    /// </summary>
    public static void Validate(Rules.AutomationRule rule)
    {
        Guard.NotNull(rule);

        ValidateForActivation(rule.Name, rule.Configuration);
    }
}
