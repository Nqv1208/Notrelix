using System.Text.Json;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Automation.RulesEngine;

public static class AutomationTriggerValidator
{
    public static void Validate(AutomationTriggerDefinition trigger)
    {
        switch (trigger.Type)
        {
            case "ScheduleTrigger":
                ValidateScheduleConfig(trigger.Configuration);
                break;
            case "FieldChanged":
                ValidateFieldChangedConfig(trigger.Configuration);
                break;
            case "ItemMovedToGroup":
                ValidateItemMovedToGroupConfig(trigger.Configuration);
                break;
            case "ItemCreated":
            case "ItemUpdated":
            case "ItemDeleted":
            case "FormSubmitted":
            case "ItemAssigned":
                ValidateHasJsonConfig(trigger.Configuration);
                break;
            default:
                throw new BusinessRuleException($"Unknown trigger type '{trigger.Type}'.");
        }
    }

    private static void ValidateHasJsonConfig(string? configuration)
    {
        if (configuration is null) return;
        try
        {
            using var doc = JsonDocument.Parse(configuration);
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid trigger configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateScheduleConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException("ScheduleTrigger requires a configuration with 'cron' or 'interval' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("cron", out _) && !root.TryGetProperty("interval", out _))
                throw new BusinessRuleException("ScheduleTrigger configuration must contain 'cron' or 'interval'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid ScheduleTrigger configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateFieldChangedConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException("FieldChanged trigger requires a configuration with 'fieldId' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("fieldId", out _))
                throw new BusinessRuleException("FieldChanged trigger configuration must contain 'fieldId'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid FieldChanged configuration JSON: {ex.Message}");
        }
    }

    private static void ValidateItemMovedToGroupConfig(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
            throw new BusinessRuleException("ItemMovedToGroup trigger requires a configuration with 'groupId' or 'fromGroupId' property.");

        try
        {
            using var doc = JsonDocument.Parse(configuration);
            var root = doc.RootElement;

            if (!root.TryGetProperty("groupId", out _) && !root.TryGetProperty("fromGroupId", out _))
                throw new BusinessRuleException("ItemMovedToGroup trigger configuration must contain 'groupId' or 'fromGroupId'.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException($"Invalid ItemMovedToGroup configuration JSON: {ex.Message}");
        }
    }
}
