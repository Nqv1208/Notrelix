using System.Reflection;

namespace Notrelix.Domain.Tests.Freeze;

/// <summary>
/// Marks a test method as covering a specific mutation scenario for an aggregate.
/// Uses compiler-safe nameof + Type[] to resolve the exact method overload.
/// AllowMultiple enables one test to document multiple applicable dimensions.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class CoversMutationAttribute : Attribute
{
    public Type AggregateType { get; }
    public string MethodName { get; }
    public Type[] ParameterTypes { get; }
    public MutationScenario Scenario { get; }

    /// <summary>
    /// Creates a compiler-safe mutation coverage marker.
    /// </summary>
    /// <param name="aggregateType">The aggregate type containing the mutation.</param>
    /// <param name="methodName">Use nameof(AggregateType.MethodName) for compiler safety.</param>
    /// <param name="scenario">The mutation scenario being covered.</param>
    /// <param name="parameterTypes">The exact parameter types to resolve the overload.</param>
    public CoversMutationAttribute(
        Type aggregateType,
        string methodName,
        MutationScenario scenario,
        params Type[] parameterTypes)
    {
        AggregateType = aggregateType ?? throw new ArgumentNullException(nameof(aggregateType));
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        MethodName = methodName;
        Scenario = scenario;
        ParameterTypes = parameterTypes ?? Array.Empty<Type>();
    }

    /// <summary>
    /// Resolves the exact MethodInfo for this mutation using reflection.
    /// Returns null if the method does not exist.
    /// </summary>
    public MethodInfo? ResolveMethod()
    {
        return AggregateType.GetMethod(
            MethodName,
            BindingFlags.Public | BindingFlags.Instance,
            null,
            ParameterTypes,
            null);
    }

    /// <summary>
    /// Formats the method signature for display and comparison.
    /// Uses MutationSignatureFormatter for consistent formatting.
    /// </summary>
    public string FormatSignature()
    {
        var paramStr = string.Join(",", ParameterTypes.Select(t => MutationSignatureFormatter.FormatType(t)));
        return $"{MethodName}({paramStr})";
    }
}
