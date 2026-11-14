namespace Notrelix.API.Contracts.Automation.Rules.Responses;

/// <summary>
/// The 400 body of an enable that is refused because a target the rule names is archived or deleted
/// (UC-AUT-D10, UC-AUT-K04). <see cref="BrokenReferences"/> lists every such target; the problem fields
/// (title, status, detail, errorCode, ruleCode) are the standard problem response.
/// </summary>
public sealed record AutomationEnableRefusalResponse(
    string Title,
    int Status,
    string Detail,
    string ErrorCode,
    string RuleCode,
    IReadOnlyList<AutomationBrokenReferenceResponse> BrokenReferences);

/// <summary>One archived or deleted target: its kind (board, group, option, field, member), its id, and the rule field that names it.</summary>
public sealed record AutomationBrokenReferenceResponse(string Kind, Guid Id, string Field);
