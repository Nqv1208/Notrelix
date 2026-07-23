namespace Notrelix.Domain.WorkManagement.Rollups;

/// <summary>
/// EXPERIMENTAL: Rollup function types. Rollup evaluation not yet implemented.
/// Do not depend on this enum in production code paths.
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
