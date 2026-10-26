namespace Notrelix.API.Contracts.Automation.Rules.Requests;

/// <summary>One action of a rule; a rule runs up to five, in order.</summary>
public sealed record AutomationActionRequest(string Type, string? Configuration);

public sealed record CreateAutomationRuleRequest(
    string Name,
    string TriggerEvent,
    string? ActionType,
    string? Configuration,
    Guid? BoardId = null,
    string? TriggerConfiguration = null,
    IReadOnlyList<AutomationActionRequest>? Actions = null,
    string? Conditions = null);
