namespace Notrelix.Domain.Automation.Actions;

/// <summary>
/// Experimental — runtime action entity. Discriminator and config schema are not yet finalized.
/// </summary>
public class AutomationAction : Entity
{
    public Guid RuleId { get; private set; }
    public AutomationActionType Type { get; private set; }
    public ActionConfig Config { get; private set; } = null!;
    public int Position { get; private set; }

    private AutomationAction() : base() { }

    public static AutomationAction Create(Guid ruleId, AutomationActionType type, ActionConfig config, int position)
    {
        Guard.NotEmpty(ruleId);
        Guard.NotNull(config);

        return new AutomationAction
        {
            RuleId = ruleId,
            Type = type,
            Config = config,
            Position = position
        };
    }
}
