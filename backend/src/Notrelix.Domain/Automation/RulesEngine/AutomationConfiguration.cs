namespace Notrelix.Domain.Automation.RulesEngine;

public sealed class AutomationConfiguration : ValueObject
{
    public AutomationTriggerDefinition Trigger { get; private set; }
    public AutomationActionDefinition Action { get; private set; }
    public AutomationConditionDefinition? Condition { get; private set; }

    private AutomationConfiguration() { }

    private AutomationConfiguration(
        AutomationTriggerDefinition trigger,
        AutomationActionDefinition action,
        AutomationConditionDefinition? condition)
    {
        Trigger = trigger;
        Action = action;
        Condition = condition;
    }

    public static AutomationConfiguration Create(
        AutomationTriggerDefinition trigger,
        AutomationActionDefinition action,
        AutomationConditionDefinition? condition = null)
    {
        Guard.NotNull(trigger);
        Guard.NotNull(action);

        return new AutomationConfiguration(trigger, action, condition);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Trigger;
        yield return Condition;
        yield return Action;
    }

    public override string ToString() => $"Trigger: {Trigger}, Action: {Action}";
}
