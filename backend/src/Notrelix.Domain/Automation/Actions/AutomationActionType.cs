namespace Notrelix.Domain.Automation.Actions;

/// <summary>
/// Experimental — runtime action type discriminator. Values are not aligned with RulesEngine definitions.
/// </summary>
public enum AutomationActionType
{
    UpdateItem,
    CreateItem,
    SendNotification,
    SendEmail,
    CallWebhook,
    MoveItem
}
