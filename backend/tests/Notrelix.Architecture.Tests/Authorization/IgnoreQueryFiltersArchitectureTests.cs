namespace Notrelix.Architecture.Tests;

/// <summary>
/// Architecture tests auditing IgnoreQueryFilters() usage in production code.
/// Every IgnoreQueryFilters() call must be classified — no unmapped usage allowed.
/// </summary>
public class IgnoreQueryFiltersArchitectureTests
{
    private static string GetSolutionRoot()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return current;
    }

    private static string[] GetProductionCsFiles()
    {
        var root = GetSolutionRoot();
        var srcPath = Path.Combine(root, "src");
        return Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    private static List<(string File, int Line, string Method)> FindIgnoreQueryFiltersCalls(string[] files)
    {
        var results = new List<(string File, int Line, string Method)>();
        var pattern = new Regex(@"\.IgnoreQueryFilters\s*\(");

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    var methodContext = ExtractMethodContext(lines, i);
                    results.Add((Path.GetRelativePath(GetSolutionRoot(), file), i + 1, methodContext));
                }
            }
        }

        return results;
    }

    private static string ExtractMethodContext(string[] lines, int lineIndex)
    {
        for (var i = lineIndex; i >= 0; i--)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("public ") || trimmed.StartsWith("private ") ||
                trimmed.StartsWith("protected ") || trimmed.StartsWith("internal "))
            {
                var match = Regex.Match(trimmed, @"(?:async\s+)?(?:Task|void|ValueTask)\s+(\w+)");
                if (match.Success)
                    return match.Groups[1].Value;
            }
        }
        return "(unknown)";
    }

    private static readonly Dictionary<string, AllowlistEntry> IgnoreQueryFiltersAllowlist = new()
    {
        ["RestoreWorkspace.cs"] = new("RestoreWorkspace.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted workspaces to restore them",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreSpace.cs"] = new("RestoreSpace.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted spaces to restore them",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["RestoreTeam.cs"] = new("RestoreTeam.cs", AllowlistClassification.Intentional,
            "Restore operation must see soft-deleted teams to restore them",
            "Keep as Intentional — restore requires IgnoreQueryFilters"),
        ["ApplicationDbContextInitialiser.cs"] = new("ApplicationDbContextInitialiser.cs", AllowlistClassification.SystemCommand,
            "Database seed/reset must clear all tables regardless of query filters",
            "Keep as SystemCommand — infrastructure maintenance operation"),
        ["WorkspaceAccessResolver.cs"] = new("WorkspaceAccessResolver.cs", AllowlistClassification.InfrastructureBootstrap,
            "Bootstrap resolver bypasses EF query filter to resolve tenant before RLS context is set",
            "Keep as InfrastructureBootstrap — resolver runs before RLS session is established"),
        ["TenantBootstrapStore.cs"] = new("TenantBootstrapStore.cs", AllowlistClassification.InfrastructureBootstrap,
            "Tenant bootstrap store bypasses EF query filter to resolve workspace before RLS context is set",
            "Keep as InfrastructureBootstrap — store runs before RLS session is established"),

        ["ResourceLocator.cs"] = new("ResourceLocator.cs", AllowlistClassification.InfrastructureBootstrap,
            "Resource locator bypasses EF query filter to resolve resource tenant context before RLS is set",
            "Keep as InfrastructureBootstrap — resolver runs before RLS session is established"),
        ["WorkManagementResourceAuthorizationFactsProvider.cs"] = new("WorkManagementResourceAuthorizationFactsProvider.cs", AllowlistClassification.InfrastructureBootstrap,
            "WorkManagement facts adapter bypasses EF query filter to resolve a board's owning tenant/workspace before RLS context is set",
            "Keep as InfrastructureBootstrap — resource-owner facts are resolved before RLS session is established"),
        ["EmailTemplateMaterialization.cs"] = new("EmailTemplateMaterialization.cs", AllowlistClassification.Intentional,
            "Email template materialization bypasses EF query filter to resolve templates across tenant boundaries",
            "Keep as Intentional — cross-tenant template resolution"),
        ["EmailVerificationTokenIssuer.cs"] = new("EmailVerificationTokenIssuer.cs", AllowlistClassification.Intentional,
            "Token issuer bypasses EF query filter to revoke prior tokens across tenant boundaries",
            "Keep as Intentional — cross-tenant token revocation"),
        ["ActiveVerificationTokenLocker.cs"] = new("ActiveVerificationTokenLocker.cs", AllowlistClassification.Intentional,
            "Token locker bypasses EF query filter to lock active verification tokens across tenant boundaries",
            "Keep as Intentional — cross-tenant token locking"),
    };

    [Fact]
    public void IgnoreQueryFiltersUsage_ShouldBeClassified()
    {
        var files = GetProductionCsFiles();
        var calls = FindIgnoreQueryFiltersCalls(files);

        var violations = new List<string>();

        foreach (var (file, line, method) in calls)
        {
            var fileName = Path.GetFileName(file);
            if (!IgnoreQueryFiltersAllowlist.ContainsKey(fileName))
            {
                violations.Add($"{file}:{line} — method {method} — unclassified IgnoreQueryFilters() call");
            }
        }

        violations.Should().BeEmpty(
            $"All IgnoreQueryFilters() calls in production code must be classified in IgnoreQueryFiltersAllowlist. " +
            $"Add new entries with AllowlistClassification and reason. " +
            $"Violations: {string.Join("; ", violations)}");
    }

    [Fact]
    public void IgnoreQueryFiltersAllowlist_ShouldHaveNoDuplicateEntries()
    {
        var duplicates = IgnoreQueryFiltersAllowlist.Keys
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            $"IgnoreQueryFiltersAllowlist must not contain duplicate entries. Duplicates: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void IgnoreQueryFiltersAllowlist_ShouldHaveNoFalsePositiveClassifications()
    {
        var falsePositives = IgnoreQueryFiltersAllowlist.Values
            .Where(e => e.Classification == AllowlistClassification.FalsePositive)
            .Select(e => e.RequestTypeName)
            .ToList();

        falsePositives.Should().BeEmpty(
            $"IgnoreQueryFiltersAllowlist must not contain FalsePositive entries. " +
            $"If an IgnoreQueryFilters() call is truly a false positive, remove it from production code instead. " +
            $"FalsePositives: {string.Join(", ", falsePositives)}");
    }
}
