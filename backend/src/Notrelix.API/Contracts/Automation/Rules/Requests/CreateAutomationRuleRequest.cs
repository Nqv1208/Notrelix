namespace Notrelix.API.Contracts.Automation.Rules.Requests;

public sealed record CreateAutomationRuleRequest(
    string Name,
    string TriggerEvent,
    string ActionType,
    string? Configuration,
    Guid? BoardId = null,
    string? TriggerConfiguration = null);