using System.Reflection;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// IDEM-META-001..006: Guards idempotency operation metadata contract.
/// </summary>
public class IdempotencyOperationArchitectureTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(Notrelix.Application.Common.Idempotency.IdempotencyOperationAttribute).Assembly;

    private static readonly Regex OperationFormatRegex =
        new(@"^[a-z][a-z0-9-]*(\.[a-z][a-z0-9-]*){2,}\.v[1-9][0-9]*$", RegexOptions.Compiled);

    private static readonly string[] ForbiddenSegments =
        ["Notrelix", "Features", "Command", "Handler"];

    private static IEnumerable<Type> GetIdempotentRequestTypes()
    {
        return ApplicationAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => typeof(IIdempotentRequest).IsAssignableFrom(t));
    }

    [Fact]
    public void IDEM_META_001_Every_Concrete_Idempotent_Request_Has_Exactly_One_Operation_Attribute()
    {
        var types = GetIdempotentRequestTypes();

        var missing = types
            .Where(t => t.GetCustomAttribute<IdempotencyOperationAttribute>() is null)
            .Select(t => t.FullName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "every concrete IIdempotentRequest must declare [IdempotencyOperation] with a stable literal");
    }

    [Fact]
    public void IDEM_META_002_Operations_Are_Unique_Across_Application()
    {
        var types = GetIdempotentRequestTypes();

        var operations = new Dictionary<string, string>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<IdempotencyOperationAttribute>();
            if (attr is null) continue;

            if (operations.TryGetValue(attr.Operation, out var existing))
            {
                duplicates.Add($"'{attr.Operation}' declared on both '{existing}' and '{type.FullName}'");
            }
            else
            {
                operations[attr.Operation] = type.FullName!;
            }
        }

        duplicates.Should().BeEmpty("idempotency operations must be globally unique");
    }

    [Fact]
    public void IDEM_META_003_Operations_Match_Required_Format()
    {
        var types = GetIdempotentRequestTypes();

        var invalid = types
            .Select(t => t.GetCustomAttribute<IdempotencyOperationAttribute>()?.Operation)
            .Where(op => op is not null && !OperationFormatRegex.IsMatch(op))
            .ToArray();

        invalid.Should().BeEmpty(
            "operations must match {{context}}.{{module}}.{{action}}.v{{N}} (lowercase kebab-dot, min 3 segments before version)");
    }

    [Fact]
    public void IDEM_META_004_Operations_Do_Not_Contain_Clr_Or_Layer_Words()
    {
        var types = GetIdempotentRequestTypes();

        var violations = types
            .Select(t => t.GetCustomAttribute<IdempotencyOperationAttribute>()?.Operation)
            .Where(op => op is not null)
            .Where(op => ForbiddenSegments.Any(seg =>
                op!.Contains(seg, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        violations.Should().BeEmpty(
            "operations must not contain CLR/layer words: Notrelix, Features, Command, Handler");
    }

    [Fact]
    public void IDEM_META_005_No_Query_Implements_Idempotent_Marker()
    {
        var queryTypes = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>) &&
                i.GetGenericArguments()[0].Name.StartsWith("Result")))
            .Where(t => t.Name.EndsWith("Query", StringComparison.Ordinal))
            .Where(t => typeof(IIdempotentRequest).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToArray();

        queryTypes.Should().BeEmpty("queries must not implement IIdempotentRequest");
    }

    [Fact]
    public void IDEM_META_006_Metadata_Resolution_Throws_For_Missing_Attribute()
    {
        var act = () => IdempotencyOperationMetadata.Resolve(typeof(RequestWithoutAttribute));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*missing [IdempotencyOperation]*");
    }

    private sealed record RequestWithoutAttribute(string IdempotencyKey) : IIdempotentRequest;
}
