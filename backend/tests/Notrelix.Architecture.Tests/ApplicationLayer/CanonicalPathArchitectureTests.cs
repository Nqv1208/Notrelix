namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// APP-PATH-001..007: Enforces canonical Application layout.
/// Canonical: Features/{Context}/{Module}/Commands|Queries/{UseCase}/
/// Forbidden: Features/{Context}/Commands|Queries/{Module}/ (legacy)
/// Public surfaces: Features/{Context}/Public/{PublishedCapability}/
///   (capability-first; technical buckets are frozen legacy debt only, see §0.1A)
/// </summary>
public class CanonicalPathArchitectureTests
{
    [Fact]
    public void APP_PATH_001_No_Legacy_Commands_Or_Queries_At_Context_Level()
    {
        var featuresPath = GetFeaturesPath();

        var contextDirs = Directory.GetDirectories(featuresPath)
            .Where(d => !Path.GetFileName(d).StartsWith('.'))
            .ToList();

        var violations = new List<string>();

        foreach (var contextDir in contextDirs)
        {
            var subDirs = Directory.GetDirectories(contextDir)
                .Select(Path.GetFileName)
                .ToList();

            if (subDirs.Contains("Commands") || subDirs.Contains("Queries"))
            {
                var contextName = Path.GetFileName(contextDir);
                violations.Add(
                    $"Features/{contextName}/ contains Commands or Queries directly — " +
                    "must be Features/{contextName}/{module}/Commands|Queries/");
            }
        }

        violations.Should().BeEmpty(
            "Application must use canonical module-first layout: " +
            "Features/{Context}/{Module}/Commands|Queries/{UseCase}/");
    }

    [Fact]
    public void APP_PATH_004_All_Handler_Files_Under_Commands_Or_Queries()
    {
        var featuresPath = GetFeaturesPath();

        var csFiles = Directory.GetFiles(featuresPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToList();

        var violations = new List<string>();

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            if (!content.Contains("IRequestHandler<"))
                continue;

            var relativePath = Path.GetRelativePath(featuresPath, file);
            var parts = relativePath.Split(Path.DirectorySeparatorChar);

            var hasCommandsOrQueries = parts.Any(p =>
                p is "Commands" or "Queries");

            if (!hasCommandsOrQueries)
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            "all handler files must be under Commands/ or Queries/ subdirectories " +
            "(application-model.md §5); Services/ and Abstractions/ are not handler containers");
    }

    [Fact]
    public void APP_PATH_006_Public_Technical_Buckets_Are_Exact_Frozen_Legacy_Baseline()
    {
        var featuresPath = GetFeaturesPath();

        var forbiddenBuckets = new[]
        {
            "Commands", "Queries", "Facts", "Actions",
            "Contracts", "DTOs", "Services", "Common"
        };

        var frozenLegacyBaseline = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Billing"] = new[] { "Facts" },
            ["Identity"] = new[] { "Facts", "Queries" },
            ["Integrations"] = new[] { "Commands" },
            ["WorkManagement"] = new[] { "Commands", "Queries" }
        };

        var violations = new List<string>();

        foreach (var contextDir in Directory.GetDirectories(featuresPath))
        {
            var contextName = Path.GetFileName(contextDir);
            var publicDir = Path.Combine(contextDir, "Public");
            if (!Directory.Exists(publicDir))
                continue;

            foreach (var surfaceDir in Directory.GetDirectories(publicDir))
            {
                var surfaceName = Path.GetFileName(surfaceDir);
                if (!forbiddenBuckets.Contains(surfaceName))
                    continue;

                var isFrozenLegacy =
                    frozenLegacyBaseline.TryGetValue(contextName, out var frozenBuckets)
                    && frozenBuckets.Contains(surfaceName);

                if (!isFrozenLegacy)
                {
                    violations.Add($"Features/{contextName}/Public/{surfaceName}");
                }
            }
        }

        violations.Should().BeEmpty(
            "Application Public surfaces must be capability-first " +
            "(Features/{Context}/Public/{PublishedCapability}/). Top-level technical buckets " +
            "(Commands/Queries/Facts/Actions/Contracts/DTOs/Services/Common) are non-canonical " +
            "per backend-team-architecture-closure spec §0.1A. Frozen legacy debt is limited to " +
            "Billing/Facts, Identity/{Facts,Queries}, Integrations/Commands, " +
            "WorkManagement/{Commands,Queries} and must shrink when each owning milestone normalizes " +
            "its Public surface, never grow.");
    }

    [Fact]
    public void APP_PATH_007_Accounts_Public_Surface_Is_Canonical_Capability_First()
    {
        var featuresPath = GetFeaturesPath();
        var accountsPublic = Path.Combine(featuresPath, "Accounts", "Public");

        var requiredTopology = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Membership"] = new[]
            {
                "IAccountMembershipFacts.cs",
                "AccountMembershipAdmissionFact.cs",
                "IAccountMembershipActions.cs"
            },
            ["PersonalAccountProvisioning"] = new[]
            {
                "IAccountProvisioningActions.cs",
                "PersonalAccountProvisioningResult.cs"
            }
        };

        var presentSurfaceDirs = Directory.Exists(accountsPublic)
            ? Directory.GetDirectories(accountsPublic).Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal)
            : new HashSet<string>(StringComparer.Ordinal);

        var missingSurfaces = requiredTopology.Keys
            .Where(surface => !presentSurfaceDirs.Contains(surface))
            .ToList();

        missingSurfaces.Should().BeEmpty(
            "Accounts/Public must expose published capabilities per tac-v26 execution-status §0.4A: " +
            "Membership/{IAccountMembershipFacts,AccountMembershipAdmissionFact,IAccountMembershipActions} " +
            "and PersonalAccountProvisioning/{IAccountProvisioningActions,PersonalAccountProvisioningResult}. " +
            "Technical-bucket Public/Commands|Queries|Facts layout is non-canonical for Accounts.");

        foreach (var (capability, files) in requiredTopology)
        {
            foreach (var file in files)
            {
                var expectedPath = Path.Combine(accountsPublic, capability, file);
                File.Exists(expectedPath).Should().BeTrue(
                    $"Accounts/Public/{capability}/{file} is required by tac-v26 execution-status §0.4A " +
                    "canonical capability-first layout.");
            }
        }
    }

    private static string GetFeaturesPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }

        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");

        return Path.Combine(current, "src", "Notrelix.Application", "Features");
    }
}
