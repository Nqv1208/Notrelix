using System.Text.Json;

namespace Notrelix.Domain.Automation.RulesEngine;

public sealed class AutomationTriggerDefinition : ValueObject
{
    private static readonly HashSet<string> ValidTriggers = new(StringComparer.OrdinalIgnoreCase)
    {
        "FieldChanged", "ItemCreated", "ItemUpdated", "ItemDeleted",
        "ItemMovedToGroup", "FormSubmitted", "ScheduleTrigger",
        "ItemAssigned"
    };

    public string Type { get; private set; }
    public string? Configuration { get; private set; }

    private AutomationTriggerDefinition() { }

    private AutomationTriggerDefinition(string type, string? configuration)
    {
        Type = type;
        Configuration = configuration;
    }

    public static AutomationTriggerDefinition Create(string type, string? configuration = null)
    {
        Guard.NotNullOrWhiteSpace(type);
        Guard.Assert(ValidTriggers.Contains(type), $"Invalid trigger type '{type}'. Valid types: {string.Join(", ", ValidTriggers)}");

        if (configuration is not null)
        {
            try
            {
                using var document = JsonDocument.Parse(configuration);
                if (document.RootElement.ValueKind == JsonValueKind.Null)
                    throw new BusinessRuleException(BusinessRuleCodes.Automation_Trigger_ConfigCannotBeNullJson, "Trigger configuration cannot be null JSON.");
            }
            catch (JsonException ex)
            {
                throw new BusinessRuleException($"Invalid trigger configuration JSON: {ex.Message}");
            }
        }

        return new AutomationTriggerDefinition(type, configuration);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Configuration;
    }

    public override string ToString() => Type;
}
