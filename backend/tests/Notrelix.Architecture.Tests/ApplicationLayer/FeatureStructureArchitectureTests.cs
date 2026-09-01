namespace Notrelix.Architecture.Tests;

using System.Reflection;

/// <summary>
/// Structural topology gates from the backend structural topology execution.
/// These are filesystem/namespace checks for inherently structural rules;
/// type/dependency rules remain owned by the existing semantic gates
/// (CrossContextBoundaryScanner suites, PublicSemanticContractArchitectureTests).
///
///   STN-ARCH-001 — no feature .gitkeep placeholder scaffolding
///   STN-ARCH-002 — known real consumer ports live under consumer Ports/
///   STN-ARCH-005 — legacy Infrastructure/Data/ReadPorts does not regrow
///   STN-ARCH-006 — legacy Infrastructure/Services cannot grow (exact baseline)
///   STN-ARCH-007 — no marker-only canonical boundary folders
///   STN-ARCH-008 — old normalized seam namespaces/types have disappeared
/// </summary>
public class FeatureStructureArchitectureTests
{
    private const string RuleNoScaffolding = "STN-ARCH-001";
    private const string RulePortPlacement = "STN-ARCH-002";
    private const string RuleLegacyReadPorts = "STN-ARCH-005";
    private const string RuleServicesGrowth = "STN-ARCH-006";
    private const string RuleEmptyCanonicalFolders = "STN-ARCH-007";
    private const string RuleOldSeams = "STN-ARCH-008";

    private static string GetBackendRoot()
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

    private static string FeaturesRoot => Path.Combine(GetBackendRoot(), "src", "Notrelix.Application", "Features");

    private static string InfrastructureRoot => Path.Combine(GetBackendRoot(), "src", "Notrelix.Infrastructure");

    // ------------------------------------------------------------------
    // STN-ARCH-001
    // ------------------------------------------------------------------

    [Fact]
    public void NoFeatureGitkeepPlaceholders()
    {
        var placeholders = Directory
            .EnumerateFiles(FeaturesRoot, ".gitkeep", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName!)
            .Select(d => Path.GetRelativePath(FeaturesRoot, d))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        placeholders.Should().BeEmpty(
            $"{RuleNoScaffolding}: placeholder feature topology detected at [{string.Join(", ", placeholders)}]. " +
            "Do not pre-create architecture folders; create the folder together with its first real source type.");
    }

    // ------------------------------------------------------------------
    // STN-ARCH-002
    // ------------------------------------------------------------------

    [Fact]
    public void KnownConsumerPorts_UseCanonicalPaths()
    {
        var required = new (string Type, string RelativePath, string ExpectedNamespace)[]
        {
            ("IWorkManagementCollaborationReadPort",
             Path.Combine("WorkManagement", "Ports", "Collaboration", "IWorkManagementCollaborationReadPort.cs"),
             "Notrelix.Application.Features.WorkManagement.Ports.Collaboration"),
            ("IIdentityBootstrapReadPort",
             Path.Combine("Identity", "Ports", "Bootstrap", "IIdentityBootstrapReadPort.cs"),
             "Notrelix.Application.Features.Identity.Ports.Bootstrap"),
        };

        var applicationTypes = Assembly.Load("Notrelix.Application").GetTypes();

        foreach (var (type, relativePath, expectedNamespace) in required)
        {
            File.Exists(Path.Combine(FeaturesRoot, relativePath)).Should().BeTrue(
                $"{RulePortPlacement}: {type} must live at Features/{relativePath.Replace('\\', '/')}");

            var consumer = applicationTypes.SingleOrDefault(t => t.Name == type);
            consumer.Should().NotBeNull($"{type} is a real production consumer port");
            consumer!.Namespace.Should().Be(
                expectedNamespace,
                $"{RulePortPlacement}: {type} belongs under its consumer's Ports tree");
        }
    }

    // ------------------------------------------------------------------
    // STN-ARCH-005
    // ------------------------------------------------------------------

    [Fact]
    public void LegacyDataReadPorts_DoesNotRegrow()
    {
        var legacyPath = Path.Combine(InfrastructureRoot, "Data", "ReadPorts");

        Directory.Exists(legacyPath).Should().BeFalse(
            $"{RuleLegacyReadPorts}: legacy cross-context read adapter location " +
            $"Data/ReadPorts must not regrow. Classify the adapter and place it under " +
            "an intentional Infrastructure boundary location (CrossContext/...).");
    }

    // ------------------------------------------------------------------
    // STN-ARCH-006
    // ------------------------------------------------------------------

    /// <summary>
    /// Exact anti-growth baseline for legacy Infrastructure/Services. The
    /// remaining files are frozen security/tenancy/runtime mechanisms pending
    /// their own classification; a new file here requires deliberate review
    /// and a baseline update in this test.
    /// </summary>
    private static readonly IReadOnlySet<string> AllowedInfrastructureServices =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "AccountAccessEvaluator.cs",
            "ActorLookupService.cs",
            "CurrentCorrelationContext.cs",
            "ResourceLocator.cs",
            "TenantBootstrapStore.cs",
            "WorkspaceAccessChecker.cs",
            "WorkspaceAccessResolver.cs",
        };

    [Fact]
    public void InfrastructureServices_CannotGrow()
    {
        var servicesPath = Path.Combine(InfrastructureRoot, "Services");

        var actual = Directory.Exists(servicesPath)
            ? Directory
                .EnumerateFiles(servicesPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .Where(name => name != null)
                .ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        // Monotonic exact-baseline semantics: actual == reviewed baseline.
        // Deleting a source file requires shrinking the baseline in the same
        // change; a deleted file must not silently regrow or linger in the
        // baseline as a stale reusable allowance.
        var unknown = actual.Except(AllowedInfrastructureServices).ToList();
        var stale = AllowedInfrastructureServices.Except(actual).ToList();

        unknown.Should().BeEmpty(
            $"{RuleServicesGrowth}: new generic Infrastructure/Services files are forbidden " +
            $"(actual-not-in-baseline: [{string.Join(", ", unknown)}]). Classify the type and place it " +
            "under its owning context or an intentional boundary location, or update the " +
            "reviewed baseline in this test.");

        stale.Should().BeEmpty(
            $"{RuleServicesGrowth}: baseline entries without matching source are stale allowances " +
            $"(baseline-not-in-actual: [{string.Join(", ", stale)}]). Shrink the reviewed baseline " +
            "in the same change that removes the source file.");
    }

    // ------------------------------------------------------------------
    // STN-ARCH-006 gate self-tests (exact-baseline semantics)
    // ------------------------------------------------------------------

    [Fact]
    public void ServicesBaseline_MatchesExactly_Passes()
    {
        var actual = new HashSet<string>(StringComparer.Ordinal) { "A.cs", "B.cs" };
        var baseline = new HashSet<string>(StringComparer.Ordinal) { "A.cs", "B.cs" };

        actual.Except(baseline).Should().BeEmpty();
        baseline.Except(actual).Should().BeEmpty();
    }

    [Fact]
    public void ServicesBaseline_MissingSource_FailsAsStale()
    {
        // baseline {A,B}, actual {A}: deleted source without baseline shrink.
        var actual = new HashSet<string>(StringComparer.Ordinal) { "A.cs" };
        var baseline = new HashSet<string>(StringComparer.Ordinal) { "A.cs", "B.cs" };

        baseline.Except(actual).Should().NotBeEmpty("a deleted file must shrink the baseline");
    }

    [Fact]
    public void ServicesBaseline_NewFile_FailsAsUnknown()
    {
        // baseline {A}, actual {A,B}: regrowth beyond the reviewed baseline.
        var actual = new HashSet<string>(StringComparer.Ordinal) { "A.cs", "B.cs" };
        var baseline = new HashSet<string>(StringComparer.Ordinal) { "A.cs" };

        actual.Except(baseline).Should().NotBeEmpty("a new generic Services file requires review");
    }

    // ------------------------------------------------------------------
    // STN-ARCH-007
    // ------------------------------------------------------------------

    [Fact]
    public void NoMarkerOnlyCanonicalFolders()
    {
        var canonicalNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Public", "Ports", "CrossContext", "Processes", "Projections",
        };

        var violations = new List<string>();
        foreach (var directory in Directory.EnumerateDirectories(GetBackendRoot(), "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(directory);
            if (!canonicalNames.Contains(name))
                continue;

            var relative = Path.GetRelativePath(GetBackendRoot(), directory);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || relative.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            var hasSource = Directory
                .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Any();

            if (!hasSource)
                violations.Add(relative);
        }

        violations.Should().BeEmpty(
            $"{RuleEmptyCanonicalFolders}: marker-only canonical folders detected: " +
            $"[{string.Join(", ", violations)}]. Canonical folders exist only with their first real type.");
    }

    // ------------------------------------------------------------------
    // STN-ARCH-008
    // ------------------------------------------------------------------

    private static readonly IReadOnlySet<string> ForbiddenOldSeamTokens =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "WorkManagementCollaborationReadPort",      // renamed to WorkManagementCollaborationReadAdapter
            "IIdentityUserLookupService",
            "IdentityUserLookupService",
            "IdentityUserSnapshot",
            "IAccountStatusReader",
            "AccountStatusReader",
            "Features.WorkManagement.Common.Abstractions",
            "Data.ReadPorts",
        };

    [Fact]
    public void OldSeamNamespaces_HaveZeroProductionReferences()
    {
        var patterns = ForbiddenOldSeamTokens
            .Select(token => new Regex($@"(?<![A-Za-z0-9_]){Regex.Escape(token)}(?![A-Za-z0-9_])", RegexOptions.Compiled))
            .ToList();

        var violations = new List<string>();

        foreach (var project in new[] { "Notrelix.Application", "Notrelix.Infrastructure", "Notrelix.API", "Notrelix.Platform" })
        {
            var projectRoot = Path.Combine(GetBackendRoot(), "src", project);
            if (!Directory.Exists(projectRoot))
                continue;

            foreach (var file in Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var content = RemoveComments(File.ReadAllText(file));
                foreach (var pattern in patterns)
                {
                    var match = pattern.Match(content);
                    if (match.Success)
                        violations.Add($"{Path.GetRelativePath(GetBackendRoot(), file)} references '{match.Value}'");
                }
            }
        }

        violations.Should().BeEmpty(
            $"{RuleOldSeams}: old normalized seam references must disappear from production source.\n" +
            string.Join("\n", violations));
    }

    private static string RemoveComments(string input)
    {
        var cleaned = Regex.Replace(input, @"/\*(.*?)\*/", string.Empty, RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, @"//(.*?)\r?\n", "\n");
        return cleaned;
    }
}
