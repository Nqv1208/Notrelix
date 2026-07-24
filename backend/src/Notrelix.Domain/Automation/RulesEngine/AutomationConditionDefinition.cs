using System.Text.Json;

namespace Notrelix.Domain.Automation.RulesEngine;

public sealed class AutomationConditionDefinition : ValueObject
{
    public string RawJson { get; private set; } = null!;

    private AutomationConditionDefinition() { }

    private AutomationConditionDefinition(string rawJson)
    {
        RawJson = rawJson;
    }

    public static AutomationConditionDefinition Create(string json)
    {
        Guard.NotNullOrWhiteSpace(json);

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new BusinessRuleException(BusinessRuleCodes.Automation_Condition_ConfigMustBeValidJson, "Condition configuration must be a valid JSON object.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(BusinessRuleCodes.Automation_Condition_InvalidConfigJson, $"Invalid condition configuration JSON: {ex.Message}");
        }

        return new AutomationConditionDefinition(json);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RawJson;
    }

    public override string ToString() => RawJson;
}
