using System.Text.Json;

namespace Notrelix.Domain.Automation.RulesEngine;

public static class AutomationActionValidator
{
    public static void Validate(AutomationActionDefinition action)
    {
        switch (action.Type)
        {
            case "Webhook":
                ValidateWebhookConfig(action.Configuration);
                break;
            case "SendEmail":
                ValidateSendEmailConfig(action.Configuration);
                break;
            case "SlackMessage":
                ValidateSlackMessageConfig(action.Configuration);
                break;
            case "UpdateField":
                ValidateUpdateFieldConfig(action.Configuration);
                break;
            case "CreateItem":
                ValidateCreateItemConfig(action.Configuration);
                break;
            case "MoveItem":
                ValidateMoveItemConfig(action.Configuration);
                break;
            case "ChangeStatus":
                ValidateRequiredIds(action.Configuration, "ChangeStatus", "optionId");
                break;
            case "AssignPerson":
                ValidateRequiredIds(action.Configuration, "AssignPerson", "userId");
                break;
            case "NotifyMember":
                ValidateNotifyMemberConfig(action.Configuration);
                break;
            default:
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_UnknownActionType, $"Unknown action type '{action.Type}'.");
        }
    }

    private static void ValidateRequiredIds(string? configuration, string actionType, string property)
    {
        var message = $"{actionType} action configuration must contain a '{property}'.";
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Action_InvalidConfigJson, message);
        try
        {
            using var doc = JsonDocument.Parse(configuration);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty(property, out var id)
                || id.ValueKind != JsonValueKind.String
                || !Guid.TryParse(id.GetString(), out var parsed)
                || parsed == Guid.Empty)
                throw new BusinessRuleException(AutomationRuleCodes.Automation_Action_InvalidConfigJson, message);
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Action_InvalidConfigJson, $"Invalid {actionType} configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateWebhookConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidWebhookJson, "Webhook action requires a configuration with a 'webhookPath' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            // AUT-028: the Webhook action is an allow-listed n8n capability. Only a
            // relative 'webhookPath' is executable; 'url' is never used at execution
            // and absolute URLs are not supported, so it is rejected, not ignored.
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("url", out _))
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidWebhookJson, "Webhook action configuration must not contain 'url'; use 'webhookPath'.");

            // The path is appended to the internal n8n address, so traversal,
            // query/fragment, encoding and scheme/host characters are rejected.
            if (!root.TryGetProperty("webhookPath", out var path)
                || path.ValueKind != JsonValueKind.String
                || !AutomationWebhookPath.IsValid(path.GetString()))
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidWebhookJson, "Webhook action configuration must contain a 'webhookPath' of one or more segments of letters, digits, '_' or '-' (max 64 characters each) separated by '/'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidWebhookJson, $"Invalid Webhook configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateSendEmailConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidSendEmailJson, "SendEmail action requires a configuration with 'templateId' or 'subject' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("templateId", out _) && !root.TryGetProperty("subject", out _))
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidSendEmailJson, "SendEmail action configuration must contain 'templateId' or 'subject'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidSendEmailJson, $"Invalid SendEmail configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateSlackMessageConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidSlackMessageJson, "SlackMessage action requires a configuration with 'channel' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("channel", out _))
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidSlackMessageJson, "SlackMessage action configuration must contain 'channel'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidSlackMessageJson, $"Invalid SlackMessage configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateUpdateFieldConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidUpdateFieldJson, "UpdateField action requires a configuration with 'fieldId' and 'value' properties.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("fieldId", out _) || !root.TryGetProperty("value", out _))
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidUpdateFieldJson, "UpdateField action configuration must contain 'fieldId' and 'value'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidUpdateFieldJson, $"Invalid UpdateField configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateCreateItemConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidCreateItemJson, "CreateItem action requires a configuration with 'targetGroupId' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("targetGroupId", out _))
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidCreateItemJson, "CreateItem action configuration must contain 'targetGroupId'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidCreateItemJson, $"Invalid CreateItem configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateMoveItemConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidMoveItemJson, "MoveItem action requires a configuration with 'targetGroupId' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("targetGroupId", out _))
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidMoveItemJson, "MoveItem action configuration must contain 'targetGroupId'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidMoveItemJson, $"Invalid MoveItem configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateNotifyMemberConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidNotifyMemberJson, "NotifyMember action requires a configuration with 'userId' or 'teamId' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("userId", out _) && !root.TryGetProperty("teamId", out _))
                throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidNotifyMemberJson, "NotifyMember action configuration must contain 'userId' or 'teamId'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_ActionValidator_InvalidNotifyMemberJson, $"Invalid NotifyMember configuration JSON: {ex.Message}");
        }
    }
}
