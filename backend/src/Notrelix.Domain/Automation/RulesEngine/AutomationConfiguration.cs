namespace Notrelix.Domain.Automation.RulesEngine;

public sealed class AutomationConfiguration : ValueObject
{
    /// <summary>One trigger, then at most this many actions run in order (no branching, no loops).</summary>
    public const int MaxActions = 5;

    public AutomationTriggerDefinition Trigger { get; private set; } = null!;

    /// <summary>The first action. Rules made before a rule could have several carry only this one.</summary>
    public AutomationActionDefinition Action { get; private set; } = null!;

    /// <summary>The actions that run after <see cref="Action"/>, in order.</summary>
    public List<AutomationActionDefinition> MoreActions { get; private set; } = [];

    public AutomationConditionDefinition? Condition { get; private set; }
    public int SchemaVersion { get; private set; }

    /// <summary>Every action in the order it runs.</summary>
    public IReadOnlyList<AutomationActionDefinition> Actions => [Action, .. MoreActions ?? []];

    private AutomationConfiguration() { }

    private AutomationConfiguration(
        AutomationTriggerDefinition trigger,
        AutomationActionDefinition action,
        IEnumerable<AutomationActionDefinition> moreActions,
        AutomationConditionDefinition? condition,
        int schemaVersion)
    {
        Trigger = trigger;
        Action = action;
        MoreActions = moreActions.ToList();
        Condition = condition;
        SchemaVersion = schemaVersion;
    }

    public static AutomationConfiguration Create(
        AutomationTriggerDefinition trigger,
        AutomationActionDefinition action,
        AutomationConditionDefinition? condition = null,
        IEnumerable<AutomationActionDefinition>? moreActions = null)
    {
        Guard.NotNull(trigger);
        Guard.NotNull(action);

        var more = (moreActions ?? []).ToList();
        if (more.Count + 1 > MaxActions)
            throw new BusinessRuleException(
                AutomationRuleCodes.Automation_Action_InvalidConfigJson,
                $"A rule runs at most {MaxActions} actions.");

        return new AutomationConfiguration(trigger, action, more, condition, 1);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Trigger;
        yield return Condition;
        yield return Action;
        foreach (var more in MoreActions ?? []) yield return more;
        yield return SchemaVersion;
    }

    public override string ToString() => $"Trigger: {Trigger}, Actions: {string.Join(", ", Actions)}";
}
