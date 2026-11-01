namespace Notrelix.Application.Features.Automation.Public.ActivityAttribution;

/// <summary>
/// Automation-published fact that lets another context's history say which rule made a change. Collaboration
/// words the history row; it never reads Automation persistence.
/// </summary>
public interface IAutomationActivityFacts
{
    /// <summary>
    /// The name of the rule behind an execution of the Workspace, or null when the execution or its rule is
    /// gone. A deleted rule keeps its name here, so the history of what it changed stays readable.
    /// </summary>
    Task<string?> FindRuleNameAsync(Guid workspaceId, Guid executionId, CancellationToken cancellationToken);
}
