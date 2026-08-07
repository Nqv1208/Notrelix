using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Resolves and caches <see cref="IdempotencyOperationAttribute"/> metadata per request type.
/// Throws for missing attribute — never derives CLR type name as fallback.
/// </summary>
public static partial class IdempotencyOperationMetadata
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    [GeneratedRegex(@"^[a-z][a-z0-9-]*(\.[a-z][a-z0-9-]*){2,}\.v[1-9][0-9]*$")]
    private static partial Regex OperationFormatRegex();

    public static string Resolve<TRequest>() => Resolve(typeof(TRequest));

    public static string Resolve(Type requestType)
    {
        return Cache.GetOrAdd(requestType, static type =>
        {
            var attribute = type.GetCustomAttribute<IdempotencyOperationAttribute>();
            if (attribute is null)
            {
                throw new InvalidOperationException(
                    $"Idempotent request '{type.FullName}' is missing [IdempotencyOperation] attribute. " +
                    "Operation must be an explicit stable literal, never derived from CLR type name.");
            }

            var operation = attribute.Operation;

            if (!OperationFormatRegex().IsMatch(operation))
            {
                throw new InvalidOperationException(
                    $"Idempotency operation '{operation}' on '{type.FullName}' does not match required format: " +
                    "{{context}}.{{module}}.{{action}}.v{{N}} (lowercase kebab-dot, minimum 3 segments before version).");
            }

            return operation;
        });
    }

    public static void ValidateAll(IEnumerable<Type> idempotentRequestTypes)
    {
        var operations = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var type in idempotentRequestTypes)
        {
            var operation = Resolve(type);

            if (operations.TryGetValue(operation, out var existing))
            {
                throw new InvalidOperationException(
                    $"Duplicate idempotency operation '{operation}' declared on both " +
                    $"'{existing.FullName}' and '{type.FullName}'.");
            }

            operations[operation] = type;
        }
    }
}
