using System.Text.Json;

namespace Notrelix.Domain.Automation.RulesEngine;

public sealed class AutomationActionDefinition : ValueObject
{
    private static readonly HashSet<string> ValidActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "SendEmail", "UpdateField", "CreateItem", "MoveItem",
        "NotifyMember", "Webhook", "SlackMessage"
    };

    public string Type { get; private set; } = null!;
    public string? Configuration { get; private set; }
    public int SchemaVersion { get; private set; }

    private AutomationActionDefinition() { }

    private AutomationActionDefinition(string type, string? configuration, int schemaVersion)
    {
        Type = type;
        Configuration = configuration;
        SchemaVersion = schemaVersion;
    }

    public static AutomationActionDefinition Create(string type, string? configuration = null)
    {
        Guard.NotNullOrWhiteSpace(type);
        if (!ValidActions.Contains(type))
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Action_InvalidType, $"Invalid action type '{type}'. Valid types: {string.Join(", ", ValidActions)}");

        if (configuration is not null)
        {
            try
            {
                using var document = JsonDocument.Parse(configuration);
                if (document.RootElement.ValueKind == JsonValueKind.Null)
                    throw new BusinessRuleException(AutomationRuleCodes.Automation_Action_ConfigCannotBeNullJson, "Action configuration cannot be null JSON.");
            }
            catch (JsonException ex)
            {
                throw new BusinessRuleException(AutomationRuleCodes.Automation_Action_InvalidConfigJson, $"Invalid action configuration JSON: {ex.Message}");
            }
        }

        return new AutomationActionDefinition(type, configuration, 1);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Configuration;
        yield return SchemaVersion;
    }

    public override string ToString() => Type;
}
