using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

namespace Notrelix.Domain.Tests.Freeze.Architecture;

public class DeterminismSemanticTests
{
    private static readonly Assembly DomainAssembly = typeof(AggregateRoot).Assembly;

    private static readonly string DomainProjectDir = Path.GetFullPath(
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "Notrelix.Domain"));

    private static readonly Lazy<ImmutableArray<Diagnostic>> CompilationDiagnostics = new(LoadCompilation);

    private static readonly Lazy<Compilation> DomainCompilation = new(LoadDomainCompilation);

    private static readonly HashSet<string> ForbiddenSymbols =
    [
        "System.DateTime.Now",
        "System.DateTime.UtcNow",
        "System.DateTimeOffset.Now",
        "System.DateTimeOffset.UtcNow",
        "System.Random.Shared",
        "System.Environment",
        "System.Globalization.CultureInfo.CurrentCulture",
        "System.Globalization.CultureInfo.CurrentUICulture",
        "System.Threading.Thread.CurrentThread",
    ];

    private static readonly HashSet<string> AllowlistedSources =
    [
        "DeterminismSemanticTests.cs",
    ];

    private static Compilation LoadDomainCompilation()
    {
        if (!Directory.Exists(DomainProjectDir))
            return null!;

        var csFiles = Directory.GetFiles(DomainProjectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && !f.Contains("GlobalUsings"))
            .ToArray();

        if (csFiles.Length == 0)
            return null!;

        var syntaxTrees = csFiles.Select(f =>
            CSharpSyntaxTree.ParseText(
                File.ReadAllText(f),
                path: f,
                options: new CSharpParseOptions(LanguageVersion.Latest)));

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.Location is not null && File.Exists(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "Notrelix.Domain.Analysis",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation;
    }

    private static ImmutableArray<Diagnostic> LoadCompilation()
    {
        var compilation = DomainCompilation.Value;
        return compilation is not null
            ? compilation.GetDiagnostics()
            : [];
    }

    [Fact]
    public void DomainMethods_ShouldNotCallDateTimeUtcNow()
    {
        AssertNoForbiddenMemberAccess("System.DateTime.Now", "System.DateTime.UtcNow");
    }

    [Fact]
    public void DomainMethods_ShouldNotCallDateTimeOffsetUtcNow()
    {
        AssertNoForbiddenMemberAccess("System.DateTimeOffset.Now", "System.DateTimeOffset.UtcNow");
    }

    [Fact]
    public void DomainMethods_ShouldNotUseEnvironmentMembers()
    {
        AssertNoForbiddenMemberAccess("System.Environment");
    }

    [Fact]
    public void DomainMethods_ShouldNotUseRandomShared()
    {
        AssertNoForbiddenMemberAccess("System.Random.Shared");
    }

    [Fact]
    public void DomainMethods_ShouldNotUseCultureInfoCurrentCulture()
    {
        AssertNoForbiddenMemberAccess(
            "System.Globalization.CultureInfo.CurrentCulture",
            "System.Globalization.CultureInfo.CurrentUICulture");
    }

    [Fact]
    public void DomainMethods_ShouldNotUseThreadCurrentThread()
    {
        AssertNoForbiddenMemberAccess("System.Threading.Thread.CurrentThread");
    }

    private static void AssertNoForbiddenMemberAccess(params string[] forbiddenSymbolNames)
    {
        var compilation = DomainCompilation.Value;
        if (compilation is null)
            return;

        var violations = new List<string>();

        foreach (var tree in compilation.SyntaxTrees)
        {
            var filePath = tree.FilePath;
            if (AllowlistedSources.Any(a => filePath.EndsWith(a, StringComparison.Ordinal)))
                continue;

            if (filePath.Contains("/bin/") || filePath.Contains("/obj/"))
                continue;

            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            var memberAccesses = root.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>();

            foreach (var access in memberAccesses)
            {
                var symbol = model.GetSymbolInfo(access).Symbol;
                if (symbol is null)
                    continue;

                var fullName = symbol switch
                {
                    IPropertySymbol prop => $"{prop.ContainingType.ToDisplayString()}.{prop.Name}",
                    IMethodSymbol method => $"{method.ContainingType.ToDisplayString()}.{method.Name}",
                    IFieldSymbol field => $"{field.ContainingType.ToDisplayString()}.{field.Name}",
                    _ => null,
                };

                if (fullName is null)
                    continue;

                foreach (var forbidden in forbiddenSymbolNames)
                {
                    if (fullName == forbidden || fullName.StartsWith(forbidden, StringComparison.Ordinal))
                    {
                        var lineSpan = access.GetLocation().GetLineSpan();
                        var shortPath = filePath;
                        if (shortPath.StartsWith(DomainProjectDir, StringComparison.Ordinal))
                            shortPath = shortPath[DomainProjectDir.Length..].TrimStart('/');

                        violations.Add($"{shortPath}:{lineSpan.StartLinePosition.Line + 1} -> {fullName}");
                        break;
                    }
                }
            }
        }

        violations.Should().BeEmpty(
            "domain methods must not call non-deterministic static members: " +
            string.Join("\n", violations));
    }
}
