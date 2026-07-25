namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Marks a test class/fixture as covering mutation testing for a specific aggregate root.
/// Used by the freeze gate to verify each frozen aggregate has dedicated mutation tests.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CoversAggregateAttribute : Attribute
{
    public Type AggregateType { get; }

    public CoversAggregateAttribute(Type aggregateType)
    {
        AggregateType = aggregateType;
    }
}