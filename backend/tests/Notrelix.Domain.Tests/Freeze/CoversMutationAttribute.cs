namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Marks a test method as covering a specific mutation scenario for an aggregate.
/// The gate validates that the method signature exists exactly once on the target type.
/// AllowMultiple enables one test to document multiple applicable dimensions.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class CoversMutationAttribute : Attribute
{
    public Type AggregateType { get; }
    public string MethodSignature { get; }
    public MutationScenario Scenario { get; }

    public CoversMutationAttribute(Type aggregateType, string methodSignature, MutationScenario scenario)
    {
        AggregateType = aggregateType ?? throw new ArgumentNullException(nameof(aggregateType));
        ArgumentException.ThrowIfNullOrWhiteSpace(methodSignature);
        MethodSignature = methodSignature;
        Scenario = scenario;
    }
}
