namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// APP-PATH-001..007: Enforces canonical Application layout.
/// Canonical: Features/{Context}/{Module}/Commands|Queries/{UseCase}/
/// Forbidden: Features/{Context}/Commands|Queries/{Module}/ (legacy)
/// Public surfaces: Features/{Context}/Public/{PublishedCapability}/
///   (capability-first; technical buckets are exact frozen legacy debt only,
///   see application-model.md §5)
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

        // Exact frozen legacy baseline: every file currently under a technical-bucket
        // Public surface. Shrink entries here only when the owning milestone normalizes
        // the surface; the gate enforces both directions (no growth, no silent shrink).
        var frozenLegacyBaseline = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Billing/Public/Facts"] = new[] { "IBillingCapabilityFacts.cs" },
            ["Identity/Public/Facts"] = new[] { "IdentityUserFact.cs" },
            ["Identity/Public/Queries"] = new[] { "IIdentityUserFacts.cs" },
            ["Integrations/Public/Commands"] = new[] { "IN8nWebhookActions.cs" },
            ["WorkManagement/Public/Commands"] = new[] { "IWorkItemActions.cs" },
            ["WorkManagement/Public/Queries"] = new[] { "IWorkItemProjectionSource.cs" }
        };

        var violations = new List<string>();

        // 1. Capability-first rule: no NEW top-level technical bucket under any Public surface.
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

                var relativeSurface = $"{contextName}/Public/{surfaceName}";
                if (!frozenLegacyBaseline.ContainsKey(relativeSurface))
                {
                    violations.Add(
                        $"Features/{relativeSurface} is a new technical bucket; Public must be " +
                        "capability-first (Features/{Context}/Public/{PublishedCapability}/).");
                }
            }
        }

        // 2. Frozen legacy baseline must equal the actual tree exactly, in both directions.
        foreach (var (relativeSurface, baselineFiles) in frozenLegacyBaseline)
        {
            var surfaceDir = Path.Combine(
                featuresPath,
                relativeSurface.Replace('/', Path.DirectorySeparatorChar));

            var actualFiles = Directory.Exists(surfaceDir)
                ? Directory.GetFiles(surfaceDir, "*.cs", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .ToArray()
                : Array.Empty<string>();

            foreach (var unexpected in actualFiles.Except(baselineFiles))
            {
                violations.Add($"Features/{relativeSurface}/{unexpected} exceeds the frozen legacy baseline.");
            }

            foreach (var missing in baselineFiles.Except(actualFiles))
            {
                violations.Add(
                    $"Features/{relativeSurface}/{missing} is missing; normalize the surface and shrink " +
                    "the frozen baseline in this gate when the owning milestone migrates it.");
            }
        }

        violations.Should().BeEmpty(
            "Application Public surfaces must be capability-first " +
            "(Features/{Context}/Public/{PublishedCapability}/) per application-model.md §5. Existing " +
            "technical-bucket Public paths are frozen exact debt: they may only shrink through an " +
            "explicit baseline update in this gate alongside the owning milestone's normalization, " +
            "and must never grow.");
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
            "Accounts/Public must expose published capabilities per application-model.md §5 " +
            "(capability-first Public grammar): " +
            "Membership/{IAccountMembershipFacts,AccountMembershipAdmissionFact,IAccountMembershipActions} " +
            "and PersonalAccountProvisioning/{IAccountProvisioningActions,PersonalAccountProvisioningResult}. " +
            "Technical-bucket Public/Commands|Queries|Facts layout is non-canonical for Accounts.");

        foreach (var (capability, files) in requiredTopology)
        {
            foreach (var file in files)
            {
                var expectedPath = Path.Combine(accountsPublic, capability, file);
                File.Exists(expectedPath).Should().BeTrue(
                    $"Accounts/Public/{capability}/{file} is required by application-model.md §5 " +
                    "capability-first Public layout.");
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
