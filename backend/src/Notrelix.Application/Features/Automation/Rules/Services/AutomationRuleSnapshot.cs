using System.Text.Json;

namespace Notrelix.Application.Features.Automation.Rules.Services;

public sealed record AutomationActionSnapshot(string Type, string? Configuration);

/// <summary>
/// A rule as it was when an execution started. Editing a rule creates a new version; an execution
/// queued or running keeps finishing the version it began with, so it runs from this snapshot,
/// not from the rule's current configuration.
/// </summary>
public sealed record AutomationRuleSnapshot(
    int Version,
    string Name,
    Guid? ExecutorUserId,
    Guid? BoardId,
    string TriggerType,
    string? TriggerConfiguration,
    string? Condition,
    IReadOnlyList<AutomationActionSnapshot> Actions)
{
    public static AutomationRuleSnapshot From(AutomationRule rule) => new(
        rule.ConfigurationVersion,
        rule.Name,
        rule.ExecutorUserId,
        rule.BoardId,
        rule.Configuration.Trigger.Type,
        rule.Configuration.Trigger.Configuration,
        rule.Configuration.Condition?.RawJson,
        rule.Configuration.Actions.Select(a => new AutomationActionSnapshot(a.Type, a.Configuration)).ToList());

    public string Serialize() => JsonSerializer.Serialize(this);

    public static AutomationRuleSnapshot? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var snapshot = JsonSerializer.Deserialize<AutomationRuleSnapshot>(json);
            return snapshot is { Actions.Count: > 0 } ? snapshot : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
