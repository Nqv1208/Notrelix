namespace Notrelix.API.Contracts.Automation.Rules.Requests;

/// <summary>
/// The new definition of a rule (a new version). <c>BoardId</c> names the board the caller manages
/// when they are not a workspace Owner or Admin; it must be the rule's board.
/// </summary>
public sealed record UpdateAutomationRuleRequest(
    string Name,
    string TriggerEvent,
    IReadOnlyList<AutomationActionRequest> Actions,
    string? TriggerConfiguration = null,
    string? Conditions = null,
    Guid? BoardId = null);
