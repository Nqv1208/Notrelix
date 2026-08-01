using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

namespace Notrelix.Domain.Tests.Freeze.Architecture;

/// <summary>
/// Fail-closed determinism gate for the Domain layer.
///
/// Loads the actual <c>Notrelix.Domain.csproj</c> through
/// <see cref="DomainProjectCompilation"/> (MSBuildWorkspace) and semantically
/// scans every regular Domain source document for ambient nondeterministic
/// symbol access, including <c>using static</c> references.
///
/// Fail-closed requirements:
/// - Domain project not found → fail
/// - Workspace failure → fail
/// - Null compilation → fail
/// - Compilation errors → fail
/// - Zero source documents → fail
/// - Unresolved forbidden candidate → fail
/// - Forbidden symbol access → fail
/// </summary>
public sealed class DeterminismSemanticTests :
    IClassFixture<DomainProjectCompilation>
{
    private readonly DomainProjectCompilation _domain;

    public DeterminismSemanticTests(DomainProjectCompilation domain)
    {
        _domain = domain;
    }

    private static readonly HashSet<string> ForbiddenSymbolPrefixes =
    [
        "System.DateTime.Now",
        "System.DateTime.UtcNow",
        "System.DateTimeOffset.Now",
        "System.DateTimeOffset.UtcNow",
        "System.Random.Shared",
        "System.Environment",
        "System.Globalization.CultureInfo.CurrentCulture",
        "System.Globalization.CultureInfo.CurrentUICulture",
        "System.Threading.Thread.CurrentThread"
    ];

    private static readonly HashSet<string> ForbiddenTerminalNames =
    [
        "Now",
        "UtcNow",
        "Shared",
        "Environment",
        "CurrentCulture",
        "CurrentUICulture",
        "CurrentThread"
    ];

    /// <summary>
    /// Scans all regular Domain documents once and prints every violation.
    /// </summary>
    [Fact]
    public async Task Domain_source_must_not_use_ambient_nondeterministic_symbols()
    {
        var violations = new List<string>();
        var seen = new HashSet<string>();

        foreach (var document in DomainProjectCompilation.GetRegularDocuments(_domain.Project))
        {
            var tree = await document.GetSyntaxTreeAsync();
            var root = await document.GetSyntaxRootAsync();
            var model = await document.GetSemanticModelAsync();

            if (tree is null || root is null || model is null)
            {
                violations.Add(
                    $"{document.FilePath}: syntax tree / root / semantic model unavailable");
                continue;
            }

            var relativePath = GetRelativePath(document.FilePath!);

            foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                var info = model.GetSymbolInfo(name);

                var symbol =
                    info.Symbol
                    ?? info.CandidateSymbols.SingleOrDefault();

                if (symbol is null)
                {
                    var terminal = name.Identifier.Text;
                    if (ForbiddenTerminalNames.Contains(terminal))
                    {
                        var span = name.GetLocation().GetLineSpan();
                        var key =
                            $"{relativePath}|{span.StartLinePosition.Line + 1}" +
                            $"|{span.StartLinePosition.Character + 1}|UNRESOLVED:{terminal}";

                        if (seen.Add(key))
                        {
                            violations.Add(
                                $"{relativePath}:{span.StartLinePosition.Line + 1}" +
                                $":{span.StartLinePosition.Character + 1} " +
                                $"-> UNRESOLVED FORBIDDEN CANDIDATE: {terminal}");
                        }
                    }

                    continue;
                }

                var canonical = GetCanonicalName(symbol);
                if (canonical is null)
                    continue;

                var matched = ForbiddenSymbolPrefixes.FirstOrDefault(p =>
                    canonical == p
                    || canonical.StartsWith(p, StringComparison.Ordinal));

                if (matched is not null)
                {
                    var span = name.GetLocation().GetLineSpan();
                    var key =
                        $"{relativePath}|{span.StartLinePosition.Line + 1}" +
                        $"|{span.StartLinePosition.Character + 1}|{canonical}";

                    if (seen.Add(key))
                    {
                        violations.Add(
                            $"{relativePath}:{span.StartLinePosition.Line + 1}" +
                            $":{span.StartLinePosition.Character + 1} -> {canonical}");
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "Domain source must not use ambient nondeterministic symbols: " +
            string.Join("\n", violations));
    }

    private static string? GetCanonicalName(ISymbol symbol)
    {
        return symbol switch
        {
            IPropertySymbol property =>
                $"{property.ContainingType.ToDisplayString()}.{property.Name}",

            IMethodSymbol method =>
                $"{method.ContainingType.ToDisplayString()}.{method.Name}",

            IFieldSymbol field =>
                $"{field.ContainingType.ToDisplayString()}.{field.Name}",

            INamedTypeSymbol type =>
                type.ToDisplayString(),

            _ => null
        };
    }

    private static string GetRelativePath(string filePath)
    {
        var backendRoot = RepositoryRootLocator.FindBackendRoot();
        var domainDir = Path.Combine(backendRoot, "src", "Notrelix.Domain");
        var full = Path.GetFullPath(filePath);

        if (full.StartsWith(domainDir, StringComparison.Ordinal))
            return full[domainDir.Length..].TrimStart('/', '\\');

        return filePath;
    }
}
