using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Notrelix.Architecture.Tests.Support;

/// <summary>
/// FZ-APP-04: Roslyn invocation gate.
/// Detects actual invocations of persistence and provider APIs in source files,
/// replacing the old name-based reflection check (APP_DATA_005) which could not
/// see invocations hidden inside handler methods.
/// </summary>
internal static class HandlerDataAccessInvocationGate
{
    private static readonly HashSet<string> ForbiddenMethodNames = new(StringComparer.Ordinal)
    {
        "SaveChanges",
        "SaveChangesAsync",
        "BeginTransaction",
        "BeginTransactionAsync",
        "CommitTransaction",
        "CommitTransactionAsync",
        "RollbackTransaction",
        "RollbackTransactionAsync",
        "FromSql",
        "FromSqlRaw",
        "FromSqlInterpolated",
        "ExecuteSql",
        "ExecuteSqlRaw",
        "ExecuteSqlInterpolated",
        "SqlQuery",
        "SqlQueryRaw",
    };

    private static readonly HashSet<string> ForbiddenProviderTypes = new(StringComparer.Ordinal)
    {
        "NpgsqlConnection",
        "NpgsqlCommand",
        "NpgsqlBatch",
        "NpgsqlTransaction",
        "NpgsqlDataSource",
        "NpgsqlDataSourceBuilder",
        "NpgsqlParameter",
    };

    /// <summary>
    /// Scans source text for forbidden persistence/provider API usage.
    /// Returns "file:line: member" entries in source order.
    /// </summary>
    public static IReadOnlyList<string> Scan(string source, string displayName)
    {
        var violations = new List<string>();
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            var methodName = memberAccess.Name.Identifier.ValueText;
            if (!ForbiddenMethodNames.Contains(methodName))
                continue;

            var line = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            violations.Add($"{displayName}:{line}: {methodName}");
        }

        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var typeName = creation.Type switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => null,
            };

            if (typeName is null || !ForbiddenProviderTypes.Contains(typeName))
                continue;

            var line = creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            violations.Add($"{displayName}:{line}: new {typeName}");
        }

        return violations;
    }
}
