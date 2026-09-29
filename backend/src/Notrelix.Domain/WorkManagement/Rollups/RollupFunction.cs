namespace Notrelix.Domain.WorkManagement.Rollups;

/// <summary>
/// Experimental rollup function types. Rollup evaluation is not yet
/// implemented, so production code must not depend on this enum. Promotion
/// requires an accepted product/architecture decision under
/// <c>docs/governance/decision-and-exception-policy.md</c> §36; isolation is
/// enforced by <c>ExperimentalRuntimeIsolationTests</c>.
/// </summary>
public enum RollupFunction
{
    Sum,
    Average,
    Min,
    Max,
    Count,
    CountUnique
}
