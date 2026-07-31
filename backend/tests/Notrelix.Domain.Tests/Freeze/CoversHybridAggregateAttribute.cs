namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Marks a test class/fixture as covering hybrid-scope invariants for an aggregate
/// that can exist in both Global (system) and Workspace scopes.
/// The freeze gate verifies a fixture exists and has executable tests for each
/// hybrid aggregate — it does not merely assert capability status.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CoversHybridAggregateAttribute : Attribute
{
    public Type AggregateType { get; }

    public CoversHybridAggregateAttribute(Type aggregateType)
    {
        AggregateType = aggregateType;
    }
}
