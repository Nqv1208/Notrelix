namespace Notrelix.Domain.Automation.Triggers;

/// <summary>
/// Experimental — runtime trigger type discriminator. Values are not aligned with RulesEngine definitions.
/// </summary>
public enum AutomationTriggerType
{
    ItemCreated,
    ItemUpdated,
    FieldChanged,
    StatusChanged,
    Schedule,
    Webhook
}
