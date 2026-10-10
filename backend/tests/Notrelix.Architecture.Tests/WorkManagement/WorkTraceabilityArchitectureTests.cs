namespace Notrelix.Architecture.Tests.WorkManagement;

/// <summary>
/// WM-V3-TEST-001 — semantic WMREQ coverage is enforced, not asserted by a meta
/// test. Parses docs/workstreams/executions/work-management/work-management.tests.md:
/// every WMREQ001–156 declared in the SPEC has exactly one traceability row, and
/// every row names at least one existing, non-retired, substantive test family.
/// Meta families (`BASE`, `TRACE`, `CI`, `INVENTORY`, and any family whose layer
/// is CI) never count as semantic coverage. Every plan unit row likewise names
/// only existing families.
/// </summary>
public sealed class WorkTraceabilityArchitectureTests
{
    private static readonly string[] MetaPrefixes = ["BASE", "TRACE", "CI", "INVENTORY"];
    private static readonly Regex FamilyHeading = new(@"^### ([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*-\d{3}) — ", RegexOptions.Multiline);
    private static readonly Regex RequirementRow = new(@"^\| `(WMREQ\d{3})` \| (.+?) \|\s*$", RegexOptions.Multiline);
    private static readonly Regex UnitRow = new(@"^\| `(WM-V3-[A-Z0-9-]+)` \| (.+?) \|\s*$", RegexOptions.Multiline);
    private static readonly Regex FamilyId = new(@"`([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*-\d{3})`", RegexOptions.Compiled);
    private static readonly Regex SpecRequirement = new(@"^### (WMREQ\d{3}) — ", RegexOptions.Multiline);

    [Fact]
    public void EveryWmreq_MapsToAtLeastOneSubstantiveFamily()
    {
        var tests = Read("work-management.tests.md");
        var families = ParseFamilies(tests);
        var substantive = families.Where(f => f.Value.Substantive).Select(f => f.Key).ToHashSet(StringComparer.Ordinal);
        var specRequirements = SpecRequirement.Matches(Read("work-management.spec.md")).Select(m => m.Groups[1].Value).ToList();
        var rows = RequirementRow.Matches(tests).Select(m => (Requirement: m.Groups[1].Value, Families: FamilyId.Matches(m.Groups[2].Value).Select(f => f.Groups[1].Value).ToList())).ToList();

        specRequirements.Should().HaveCount(156, "the SPEC declares WMREQ001–156");
        rows.Select(r => r.Requirement).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(specRequirements,
            "every SPEC requirement has exactly one traceability row");

        var unknown = rows.SelectMany(r => r.Families.Where(f => !families.ContainsKey(f)).Select(f => $"{r.Requirement}: unknown family {f}"));
        var uncovered = rows.Where(r => !r.Families.Any(substantive.Contains)).Select(r => r.Requirement);

        unknown.Should().BeEmpty("a traceability row may only name a declared test family");
        uncovered.Should().BeEmpty("each WMREQ needs at least one substantive, non-retired family (meta families do not count)");
    }

    [Fact]
    public void EveryPlanUnitRow_NamesOnlyDeclaredFamilies()
    {
        var tests = Read("work-management.tests.md");
        var families = ParseFamilies(tests);
        var rows = UnitRow.Matches(tests).ToList();

        rows.Should().NotBeEmpty("the unit-to-family table must be parsed (NRX-016)");
        rows.SelectMany(m => FamilyId.Matches(m.Groups[2].Value).Select(f => f.Groups[1].Value)
                .Where(f => !families.ContainsKey(f)).Select(f => $"{m.Groups[1].Value}: unknown family {f}"))
            .Should().BeEmpty();
    }

    /// <summary>
    /// DISCOVERY-ARCH-001 (SPEC WMREQ152): the mandatory P3 evidence groups cited by
    /// the PLAN statuses exist and declare at least one test, so a renamed or emptied
    /// group cannot leave a green run with zero relevant Work tests.
    /// </summary>
    private static readonly string[] MandatoryP3TestClasses =
    [
        "WorkP3ManifestArchitectureTests", "WorkPermissionActionMatrixArchitectureTests", "WorkTraceabilityArchitectureTests",
        "WorkItemPlacementIntegrationTests", "WorkChecklistIntegrationTests", "WorkExpectedVersionIntegrationTests",
        "WorkIdempotencyIntegrationTests", "WorkItemFieldValueIntegrationTests", "WorkOrderingSchemaIntegrationTests",
        "WorkOrderingPreflightIntegrationTests", "WorkOrderingScopeLockIntegrationTests", "WorkChildTableRlsIntegrationTests",
        "WorkSoftDeleteLifecycleIntegrationTests", "WorkQueryTenantIsolationIntegrationTests",
        "WorkPlacementResolverTests", "WorkOrderingRebalancerTests", "WorkOrderingMetricsTests",
        "ChecklistChildMutationTests", "FieldOptionMoveTests", "BoardFieldRenameTests", "FieldJsonBoundsTests", "BoardItemFieldValueClearTests",
        "WorkP3OpenApiShapeTests", "WorkMutationTransportRegressionTests", "ConflictProblemDetailsTests",
    ];

    [Fact]
    public void MandatoryP3TestGroups_ExistAndDeclareTests()
    {
        var root = Path.Combine(FindRepoRoot(), "backend", "tests");
        var sources = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToLookup(f => Path.GetFileNameWithoutExtension(f), StringComparer.Ordinal);

        var missing = MandatoryP3TestClasses.Where(c => !sources[c].Any(f => Regex.IsMatch(File.ReadAllText(f), @"\[(Fact|Theory)\b")));

        missing.Should().BeEmpty("a mandatory P3 evidence group must exist and execute at least one test (WMREQ152)");
    }

    private static Dictionary<string, (bool Substantive, string Layer)> ParseFamilies(string tests)
    {
        var headings = FamilyHeading.Matches(tests).ToList();
        headings.Should().HaveCountGreaterThan(50, "the family catalogue must be parsed (NRX-016)");

        var families = new Dictionary<string, (bool, string)>(StringComparer.Ordinal);
        for (var i = 0; i < headings.Count; i++)
        {
            var start = headings[i].Index;
            var end = i + 1 < headings.Count ? headings[i + 1].Index : tests.Length;
            var block = tests[start..end];
            var id = headings[i].Groups[1].Value;
            var layer = Regex.Match(block, @"\*\*Layer:\*\*\s*([^\r\n]+)").Groups[1].Value.Trim();
            var retired = Regex.IsMatch(block, @"\*\*Disposition:\*\*\s*`RETIRED", RegexOptions.IgnoreCase);
            var meta = MetaPrefixes.Any(p => id.StartsWith(p + "-", StringComparison.Ordinal)) || layer.Equals("CI", StringComparison.OrdinalIgnoreCase);
            families[id] = (!retired && !meta, layer);
        }

        return families;
    }

    private static string Read(string fileName) =>
        File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "workstreams", "executions", "work-management", fileName));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "docs", "workstreams", "executions", "work-management")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the repository root must be found");
        return dir!.FullName;
    }
}
