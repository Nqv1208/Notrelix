namespace Notrelix.Domain.Automation.Actions;

/// <summary>
/// Experimental runtime action entity. Its discriminator and config schema are
/// not finalized. Promotion requires the governance decision described in
/// <c>docs/governance/decision-and-exception-policy.md</c> §36; isolation is
/// enforced by <c>ExperimentalRuntimeIsolationTests</c>.
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
