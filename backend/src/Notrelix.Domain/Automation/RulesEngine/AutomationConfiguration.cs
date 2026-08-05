namespace Notrelix.Domain.Automation.RulesEngine;

public sealed class AutomationConfiguration : ValueObject
{
    public AutomationTriggerDefinition Trigger { get; private set; } = null!;
    public AutomationActionDefinition Action { get; private set; } = null!;
    public AutomationConditionDefinition? Condition { get; private set; }
    public int SchemaVersion { get; private set; }

    private AutomationConfiguration() { }

    private AutomationConfiguration(
        AutomationTriggerDefinition trigger,
        AutomationActionDefinition action,
        AutomationConditionDefinition? condition,
        int schemaVersion)
    {
        Trigger = trigger;
        Action = action;
        Condition = condition;
        SchemaVersion = schemaVersion;
    }

    public static AutomationConfiguration Create(
        AutomationTriggerDefinition trigger,
        AutomationActionDefinition action,
        AutomationConditionDefinition? condition = null)
    {
        Guard.NotNull(trigger);
        Guard.NotNull(action);

        return new AutomationConfiguration(trigger, action, condition, 1);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Trigger;
        yield return Condition;
        yield return Action;
        yield return SchemaVersion;
    }

    public override string ToString() => $"Trigger: {Trigger}, Action: {Action}";
}
