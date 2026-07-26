namespace Notrelix.Domain.Automation.Conditions;

/// <summary>
/// Experimental — runtime condition entity. Discriminator and config schema are not yet finalized.
/// </summary>
public class AutomationCondition : Entity
{
    public Guid RuleId { get; private set; }
    public AutomationConditionType Type { get; private set; }
    public ConditionConfig Config { get; private set; } = null!;
    public int Position { get; private set; }

    private AutomationCondition() : base() { }

    public static AutomationCondition Create(Guid ruleId, AutomationConditionType type, ConditionConfig config, int position)
    {
        Guard.NotEmpty(ruleId);
        Guard.NotNull(config);

        return new AutomationCondition
        {
            RuleId = ruleId,
            Type = type,
            Config = config,
            Position = position
        };
    }
}
