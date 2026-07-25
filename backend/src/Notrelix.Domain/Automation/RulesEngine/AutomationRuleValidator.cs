namespace Notrelix.Domain.Automation.RulesEngine;

public static class AutomationRuleValidator
{
    public static void Validate(Rules.AutomationRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Name))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Rule_NameCannotBeEmpty, "Automation rule name cannot be empty.");

        if (rule.Configuration is null)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Rule_MustHaveConfiguration, "Automation rule must have a configuration with trigger and action.");

        if (rule.Configuration.Trigger is null)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Rule_MustHaveTrigger, "Automation rule configuration must have a trigger.");

        if (rule.Configuration.Action is null)
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Rule_MustHaveAction, "Automation rule configuration must have an action.");

        AutomationTriggerValidator.Validate(rule.Configuration.Trigger);
        AutomationActionValidator.Validate(rule.Configuration.Action);
    }
}
