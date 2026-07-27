namespace Notrelix.Domain.Automation.Conditions;

/// <summary>
/// Experimental — runtime condition type discriminator. Values are not aligned with RulesEngine definitions.
/// </summary>
public enum AutomationConditionType
{
    FieldValueEquals,
    FieldValueContains,
    ActorIsUser,
    ActorInTeam,
    TimeElapsed
}
