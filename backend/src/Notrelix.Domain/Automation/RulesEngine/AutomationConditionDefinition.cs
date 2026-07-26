using System.Text.Json;

namespace Notrelix.Domain.Automation.RulesEngine;

public sealed class AutomationConditionDefinition : ValueObject
{
    public string RawJson { get; private set; } = null!;
    public int SchemaVersion { get; private set; }

    private AutomationConditionDefinition() { }

    private AutomationConditionDefinition(string rawJson, int schemaVersion)
    {
        RawJson = rawJson;
        SchemaVersion = schemaVersion;
    }

    public static AutomationConditionDefinition Create(string json)
    {
        Guard.NotNullOrWhiteSpace(json);

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new BusinessRuleException(AutomationRuleCodes.Automation_Condition_ConfigMustBeValidJson, "Condition configuration must be a valid JSON object.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AutomationRuleCodes.Automation_Condition_InvalidConfigJson, $"Invalid condition configuration JSON: {ex.Message}");
        }

        return new AutomationConditionDefinition(json, 1);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
        yield return SchemaVersion;
    }

    public override string ToString() => RawJson;
}
