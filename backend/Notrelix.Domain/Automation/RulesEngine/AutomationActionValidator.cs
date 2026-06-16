using System.Text.Json;
using Notrelix.Domain.Common.Exceptions;

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
        }
    }

    private static void ValidateWebhookConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException("Webhook action requires a configuration with 'url' or 'webhookPath' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("url", out _) && !root.TryGetProperty("webhookPath", out _))
                throw new BusinessRuleException("Webhook action configuration must contain 'url' or 'webhookPath'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid Webhook configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateSendEmailConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException("SendEmail action requires a configuration with 'templateId' or 'subject' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("templateId", out _) && !root.TryGetProperty("subject", out _))
                throw new BusinessRuleException("SendEmail action configuration must contain 'templateId' or 'subject'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid SendEmail configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateSlackMessageConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException("SlackMessage action requires a configuration with 'channel' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("channel", out _))
                throw new BusinessRuleException("SlackMessage action configuration must contain 'channel'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid SlackMessage configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateUpdateFieldConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException("UpdateField action requires a configuration with 'fieldId' and 'value' properties.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("fieldId", out _) || !root.TryGetProperty("value", out _))
                throw new BusinessRuleException("UpdateField action configuration must contain 'fieldId' and 'value'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid UpdateField configuration JSON: {ex.Message}");
        }
    }
}
