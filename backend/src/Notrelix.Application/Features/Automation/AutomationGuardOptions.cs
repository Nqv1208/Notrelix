using Microsoft.Extensions.Options;

namespace Notrelix.Application.Features.Automation;

/// <summary>
/// AUT-022 runaway protection for Automation. Bound from
/// <c>Automation:Guards</c> and validated at startup. The defaults are
/// deliberately conservative: a chain of Automation-caused facts stops after a
/// few hops, one Rule cannot start an unbounded number of executions in a
/// short window, and a Rule whose most recent executions all failed is
/// disabled instead of failing forever.
/// </summary>
public sealed class AutomationGuardOptions
{
    public const string SectionName = "Automation:Guards";

    /// <summary>
    /// Maximum depth of a chain of Automation-caused executions (an execution
    /// caused by another execution's action has depth parent + 1). A fact
    /// that would start an execution deeper than this does not start it.
    /// </summary>
    public int MaxChainDepth { get; init; } = 3;

    /// <summary>Maximum executions one Rule may start within <see cref="RateWindowSeconds"/>.</summary>
    public int MaxExecutionsPerWindow { get; init; } = 60;

    /// <summary>Length of the per-Rule execution-rate window.</summary>
    public int RateWindowSeconds { get; init; } = 60;

    /// <summary>
    /// Number of most recent finished executions (since the Rule was last
    /// enabled or edited) that must all have failed for the Rule to be
    /// disabled automatically.
    /// </summary>
    public int ConsecutiveFailureLimit { get; init; } = 5;
}

public sealed class AutomationGuardOptionsValidator : IValidateOptions<AutomationGuardOptions>
{
    public ValidateOptionsResult Validate(string? name, AutomationGuardOptions options)
    {
        var failures = new List<string>();

        if (options.MaxChainDepth is < 1 or > 10)
            failures.Add($"{AutomationGuardOptions.SectionName}:MaxChainDepth must be between 1 and 10.");
        if (options.MaxExecutionsPerWindow is < 1 or > 10_000)
            failures.Add($"{AutomationGuardOptions.SectionName}:MaxExecutionsPerWindow must be between 1 and 10000.");
        if (options.RateWindowSeconds is < 1 or > 3600)
            failures.Add($"{AutomationGuardOptions.SectionName}:RateWindowSeconds must be between 1 and 3600.");
        if (options.ConsecutiveFailureLimit is < 1 or > 100)
            failures.Add($"{AutomationGuardOptions.SectionName}:ConsecutiveFailureLimit must be between 1 and 100.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
