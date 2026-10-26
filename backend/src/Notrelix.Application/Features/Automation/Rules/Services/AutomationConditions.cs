using System.Text.Json;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;

namespace Notrelix.Application.Features.Automation.Rules.Services;

/// <summary>One "equals" test on the triggering item.</summary>
public sealed record AutomationCondition(string Kind, Guid? GroupId, Guid? OptionId, Guid? FieldId, string? ValueJson);

/// <summary>
/// A rule's optional conditions: all of them must hold (AND), each an equals test on the item's
/// group, status or one field. The stored form is
/// <c>{"all":[{"type":"group","groupId":"…"},{"type":"status","optionId":"…"},{"type":"field","fieldId":"…","value":…}]}</c>.
/// </summary>
public static class AutomationConditions
{
    public const int MaxConditions = 10;

    public static bool TryParse(string? json, out List<AutomationCondition> conditions)
    {
        conditions = [];
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("all", out var all)
                || all.ValueKind != JsonValueKind.Array
                || all.GetArrayLength() > MaxConditions)
                return false;

            foreach (var element in all.EnumerateArray())
            {
                var type = Str(element, "type");
                switch (type)
                {
                    case "group" when Id(element, "groupId") is { } group:
                        conditions.Add(new("group", group, null, null, null));
                        break;
                    case "status" when Id(element, "optionId") is { } option:
                        conditions.Add(new("status", null, option, null, null));
                        break;
                    case "field" when Id(element, "fieldId") is { } field && element.TryGetProperty("value", out var value):
                        conditions.Add(new("field", null, null, field, value.GetRawText()));
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Whether every condition holds for the item facts.</summary>
    public static bool Holds(IReadOnlyList<AutomationCondition> conditions, WorkActionItemFacts facts) =>
        conditions.All(condition => condition.Kind switch
        {
            "group" => facts.GroupId == condition.GroupId,
            "status" => facts.StatusOptionId == condition.OptionId,
            "field" => facts.FieldValues.TryGetValue(condition.FieldId!.Value, out var actual)
                && Canonical(actual) == Canonical(condition.ValueJson!),
            _ => false,
        });

    private static string Canonical(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.String
                ? document.RootElement.GetString()!
                : document.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static string? Str(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static Guid? Id(JsonElement element, string name) =>
        Guid.TryParse(Str(element, name), out var id) && id != Guid.Empty ? id : null;
}
