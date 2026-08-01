using System.Reflection;

namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// Verifies Application error code conventions:
/// - Error code classes are static and end with "ErrorCodes"
/// - Error code values are unique across all bounded contexts
/// - Error code values follow canonical format (lowercase dotted kebab)
/// </summary>
public sealed class ApplicationErrorCodeTests
{
    private static readonly Assembly ApplicationAssembly =
        typeof(Notrelix.Application.Common.Errors.CommonErrorCodes).Assembly;

    [Fact]
    public void Error_code_values_must_be_unique_across_application()
    {
        var errorCodeTypes = ApplicationAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: true, IsSealed: true })
            .Where(t => t.Name.EndsWith("ErrorCodes", StringComparison.Ordinal))
            .ToList();

        errorCodeTypes.Should().NotBeEmpty("Application must have error code classes");

        var allCodes = new Dictionary<string, string>();
        var duplicates = new List<string>();

        foreach (var type in errorCodeTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string) && f.IsLiteral);

            foreach (var field in fields)
            {
                var value = (string)field.GetRawConstantValue()!;
                var owner = $"{type.Name}.{field.Name}";

                if (allCodes.TryGetValue(value, out var existingOwner))
                    duplicates.Add($"'{value}' declared by both {existingOwner} and {owner}");
                else
                    allCodes[value] = owner;
            }
        }

        duplicates.Should().BeEmpty(
            "error code values must be unique. Duplicates:\n" +
            string.Join("\n", duplicates));
    }

    [Fact]
    public void Error_code_values_must_follow_canonical_format()
    {
        var errorCodeTypes = ApplicationAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: true, IsSealed: true })
            .Where(t => t.Name.EndsWith("ErrorCodes", StringComparison.Ordinal))
            .ToList();

        var violations = new List<string>();

        foreach (var type in errorCodeTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string) && f.IsLiteral);

            foreach (var field in fields)
            {
                var value = (string)field.GetRawConstantValue()!;

                if (value != value.ToLowerInvariant())
                    violations.Add($"{type.Name}.{field.Name} = '{value}' must be lowercase");

                if (!value.Contains('.'))
                    violations.Add($"{type.Name}.{field.Name} = '{value}' must contain a dot separator (context.code)");

                if (value.Contains(' '))
                    violations.Add($"{type.Name}.{field.Name} = '{value}' must not contain spaces");
            }
        }

        violations.Should().BeEmpty(
            "error codes must follow canonical format (lowercase dotted kebab). Violations:\n" +
            string.Join("\n", violations));
    }
}
