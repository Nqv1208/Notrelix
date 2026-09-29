namespace Notrelix.Domain.Automation.Triggers;

/// <summary>
/// Experimental runtime trigger entity. Its discriminator and config schema are
/// not finalized. Promotion requires the governance decision described in
/// <c>docs/governance/decision-and-exception-policy.md</c> §36; isolation is
/// enforced by <c>ExperimentalRuntimeIsolationTests</c>.
/// </summary>
public class AutomationTrigger : Entity
{
    public Guid RuleId { get; private set; }
    public AutomationTriggerType Type { get; private set; }
    public TriggerConfig Config { get; private set; } = null!;

    private AutomationTrigger() : base() { }

    public static AutomationTrigger Create(Guid ruleId, AutomationTriggerType type, TriggerConfig config)
    {
        Guard.NotEmpty(ruleId);
        Guard.NotNull(config);

        return new AutomationTrigger
        {
            RuleId = ruleId,
            Type = type,
            Config = config
        };
    }
}
