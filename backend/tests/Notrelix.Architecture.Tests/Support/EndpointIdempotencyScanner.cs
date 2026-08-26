using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Notrelix.Architecture.Tests;

/// <summary>
/// One endpoint registration site discovered by <see cref="EndpointIdempotencyScanner"/>.
/// </summary>
internal sealed record EndpointRegistrationSite(
    string FilePath,
    int Line,
    string MapMethodName,
    string HttpMethod,
    bool HasIdempotencyKeyMarker,
    IReadOnlyList<string> IdempotentCommands)
{
    public override string ToString() =>
        $"{FilePath}:{Line} {MapMethodName} [{HttpMethod}] " +
        $"commands=[{string.Join(", ", IdempotentCommands)}] " +
        $"WithIdempotencyKey={HasIdempotencyKeyMarker}";
}

/// <summary>
/// Roslyn syntax scanner for FZ-IDEM-05: locates minimal-API endpoint registrations
/// (Map*Get/Post/Put/Patch/Delete chains), resolves method-group handlers declared in
/// the same file, and reports which idempotent command types each site constructs.
/// Syntax-level only — no semantic model is required.
/// </summary>
internal static class EndpointIdempotencyScanner
{
    private static readonly string[] HttpVerbs = ["Get", "Post", "Put", "Patch", "Delete"];

    public static IReadOnlyList<EndpointRegistrationSite> ScanFile(
        string filePath, IReadOnlySet<string> idempotentTypeNames)
    {
        var source = File.ReadAllText(filePath);
        return ScanSource(source, idempotentTypeNames, filePath);
    }

    public static IReadOnlyList<EndpointRegistrationSite> ScanSource(
        string source, IReadOnlySet<string> idempotentTypeNames, string filePath = "inline.cs")
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        var methodsByName = root
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .GroupBy(m => m.Identifier.ValueText, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var sites = new List<EndpointRegistrationSite>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var mapName = GetMapMethodName(invocation);
            if (mapName is null)
            {
                continue;
            }

            var verb = HttpVerbs.SingleOrDefault(v => mapName.EndsWith(v, StringComparison.Ordinal));
            if (verb is null)
            {
                continue;
            }

            var chainRoot = GetChainRoot(invocation);
            var hasMarker = chainRoot
                .DescendantNodesAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .Any(m => m.Name.Identifier.ValueText == "WithIdempotencyKey");

            var commands = new SortedSet<string>(StringComparer.Ordinal);
            CollectIdempotentCommands(chainRoot, methodsByName, idempotentTypeNames, commands, new HashSet<string>(StringComparer.Ordinal));

            var lineSpan = tree.GetLineSpan(invocation.Span);
            sites.Add(new EndpointRegistrationSite(
                filePath,
                lineSpan.StartLinePosition.Line + 1,
                mapName,
                verb,
                hasMarker,
                commands.ToList()));
        }

        return sites;
    }

    private static string? GetMapMethodName(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null,
        };

        return name is not null && name.StartsWith("Map", StringComparison.Ordinal) ? name : null;
    }

    /// <summary>
    /// Walks up fluent chains: a.MapX(...).WithY(...).WithZ(...) — returns the
    /// outermost invocation of the chain that started at the given Map call.
    /// </summary>
    private static InvocationExpressionSyntax GetChainRoot(InvocationExpressionSyntax invocation)
    {
        SyntaxNode current = invocation;
        while (current.Parent is MemberAccessExpressionSyntax memberAccess
               && memberAccess.Parent is InvocationExpressionSyntax outer
               && outer.Expression == memberAccess)
        {
            current = outer;
        }

        return current as InvocationExpressionSyntax ?? invocation;
    }

    private static void CollectIdempotentCommands(
        SyntaxNode scope,
        IReadOnlyDictionary<string, List<MethodDeclarationSyntax>> methodsByName,
        IReadOnlySet<string> idempotentTypeNames,
        SortedSet<string> sink,
        HashSet<string> visitedMethods)
    {
        foreach (var creation in scope.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            var typeName = GetCreatedTypeName(creation);
            if (typeName is not null && idempotentTypeNames.Contains(typeName))
            {
                sink.Add(typeName);
            }
        }

        // `record with { ... }` reuses the incoming command parameter instead of
        // an object creation — resolve the parameter's declared type.
        foreach (var withExpression in scope.DescendantNodes().OfType<WithExpressionSyntax>())
        {
            if (withExpression.Expression is not IdentifierNameSyntax baseIdentifier)
            {
                continue;
            }

            var enclosing = withExpression.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            var parameterType = enclosing?.ParameterList.Parameters
                .FirstOrDefault(p => p.Identifier.ValueText == baseIdentifier.Identifier.ValueText)
                ?.Type?.ToString();

            if (parameterType is not null && idempotentTypeNames.Contains(parameterType))
            {
                sink.Add(parameterType);
            }
        }

        foreach (var callee in EnumerateCalleeCandidates(scope))
        {
            if (!visitedMethods.Add(callee))
            {
                continue;
            }

            if (methodsByName.TryGetValue(callee, out var declarations))
            {
                foreach (var declaration in declarations)
                {
                    CollectIdempotentCommands(declaration, methodsByName, idempotentTypeNames, sink, visitedMethods);
                }
            }
        }
    }

    /// <summary>
    /// Method-group handler arguments of the Map call and local invocations inside
    /// the scanned scope — both are resolved against same-file method declarations.
    /// </summary>
    private static IEnumerable<string> EnumerateCalleeCandidates(SyntaxNode scope)
    {
        foreach (var identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var parent = identifier.Parent;

            // Method group passed as an argument: Map("/", HandleAsync)
            if (parent is ArgumentSyntax)
            {
                yield return identifier.Identifier.ValueText;
                continue;
            }

            // Local invocation: await OtherHelperAsync(...)
            if (parent is InvocationExpressionSyntax call && call.Expression == identifier)
            {
                yield return identifier.Identifier.ValueText;
            }
        }
    }

    private static string? GetCreatedTypeName(ObjectCreationExpressionSyntax creation)
    {
        return creation.Type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            _ => null,
        };
    }
}
