namespace Notrelix.Architecture.Tests.Events;

/// <summary>
/// IA-TST-TRACE-001 / IAREQ140 / IAAC017 / IAAC021.
///
/// Execution-plan traceability gate: the canonical workstream PLAN must keep
/// a normative traceability matrix in which every declared source/closure
/// work unit and every handoff phase stays present with non-empty SPEC
/// requirement mappings, TESTS families/IDs, implementation surface and
/// CI/artifact evidence. The matrix heading searched here is the canonical
/// document's own literal section title.
///
/// Documentation traceability only — it does not replace the executable tests
/// mapped by the table and creates no second traceability authority.
/// </summary>
public class PlanTraceabilityMatrixArchitectureTests
{
    private const string MatrixHeading = "# Phase 13+ normative traceability matrix";

    private static readonly string[] RequiredWorkUnits =
    [
        "P13-CLOSE-00",
        "P13-CSRF-01",
        "P13-CSRF-02",
        "P13-CSRF-03",
        "P13-CSRF-04",
        "P13-AUTHZ-003A",
        "P13-AUTHZ-003B",
        "P13-AUTHZ-004A",
        "P13-AUTHZ-004B",
        "P13-EVT-001A",
        "P13-EVT-001B",
        "P13-EVT-002A",
        "P13-EVT-002B",
        "P13-EVT-003A",
        "P13-EVT-003B",
        "P13-EVT-003C",
        "P13-EVT-003D",
        "P13-EVT-OPS-001",
        "P13-FINAL-01",
    ];

    private static readonly string[] RequiredPhases =
    [
        "Phase 14",
        "Phase 15",
        "Phase 16",
        "Phase 17",
        "Phase 18",
        "Phase 19",
        "Phase 20",
    ];

    [Fact]
    public void TraceabilityMatrix_CoversEveryWorkUnit_AndHandoffPhase()
    {
        var planPath = FindPlanFile();
        var table = ExtractMatrixRows(planPath);

        var problems = new List<string>();

        foreach (var required in RequiredWorkUnits.Concat(RequiredPhases))
        {
            if (!table.TryGetValue(required, out var row))
            {
                problems.Add($"{required}: missing from the normative traceability matrix");
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Requirements))
            {
                problems.Add($"{required}: no SPEC requirement/IAAC mapping");
            }

            if (string.IsNullOrWhiteSpace(row.TestFamilies))
            {
                problems.Add($"{required}: no mandatory TESTS family/test ID mapping");
            }

            if (string.IsNullOrWhiteSpace(row.Surface))
            {
                problems.Add($"{required}: no primary implementation/evidence surface");
            }

            if (string.IsNullOrWhiteSpace(row.Evidence))
            {
                problems.Add($"{required}: no CI/artifact evidence mapping");
            }
        }

        // Duplicate rows would create competing traceability authority.
        var duplicateKeys = table
            .Where(kv => kv.Value.Duplicate)
            .Select(kv => kv.Key);

        problems.AddRange(duplicateKeys.Select(k => $"{k}: duplicate matrix row"));

        problems.Should().BeEmpty(
            "the canonical identity-accounts.plan.md Phase 13+ traceability matrix must map every "
            + "P13 work unit and Phase 14–20 handoff to non-empty requirements/tests/surface/evidence: "
            + string.Join("; ", problems));
    }

    [Fact]
    public void TraceabilityMatrix_TestIds_MapToCanonicalTestFamilies()
    {
        var planPath = FindPlanFile();
        var table = ExtractMatrixRows(planPath);

        var knownFamilies = new[]
        {
            "IA-TST-CLOSE", "IA-TST-TRACE", "IA-TST-CSRF", "IA-TST-X-CSRF",
            "IA-TST-AUTHZ-APP", "IA-TST-AUTHZ-ARCH", "IA-TST-AUTHZ-SEC", "IA-TST-X-AUTHZ",
            "IA-TST-PERF", "IA-TST-EVT-DOM", "IA-TST-EVT-INV", "IA-TST-EVT-INT",
            "IA-TST-EVT-VER", "IA-TST-EVT-CONTRACT", "IA-TST-EVT-MIG", "IA-TST-EVT-OPS",
            "IA-TST-EVT-SEC", "IA-TST-EVT-PRIV", "IA-TST-X-EVT", "IA-TST-MIG-EVT",
            "IA-TST-MIG-CSRF", "IA-TST-MIG-DB", "IA-TST-MIG-ACCOUNT", "IA-TST-MIG-IDENTITY",
            "IA-TST-MIG-OAUTH", "IA-TST-MIG-MFA", "IA-TST-MIG-TOKEN", "IA-TST-SEC-MASTER",
            "IA-TST-OBS", "IA-TST-REL", "IA-TST-PRIV",
        };

        bool MatchesKnownFamily(string candidate)
        {
            if (knownFamilies.Any(f => candidate.StartsWith(f, StringComparison.Ordinal)))
            {
                return true;
            }

            // Tolerate suffixed IDs (e.g. IA-TST-CSRF-API-001) by trimming
            // trailing segments until a known family prefix matches.
            var trimmed = candidate;
            while (trimmed.Contains('-'))
            {
                trimmed = trimmed[..trimmed.LastIndexOf('-')];
                if (knownFamilies.Any(f => candidate.StartsWith(f, StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }

        var violations = new List<string>();

        foreach (var (unit, row) in table)
        {
            var tokens = Regex.Split(row.TestFamilies ?? string.Empty, "[,`|\\s]+");
            foreach (var raw in tokens)
            {
                var candidate = raw.Trim('`', ' ');
                if (!candidate.StartsWith("IA-TST-", StringComparison.Ordinal))
                {
                    continue;
                }

                // Wildcard family references (e.g. IA-TST-MIG-*) are canonical.
                var isWildcard = candidate.EndsWith("*", StringComparison.Ordinal);
                var probe = isWildcard ? candidate.TrimEnd('*').TrimEnd('-') : candidate;

                if (isWildcard
                    ? !knownFamilies.Any(f => f.StartsWith(probe, StringComparison.Ordinal))
                    : !MatchesKnownFamily(candidate))
                {
                    violations.Add(
                        $"{unit}: test ID '{candidate}' is outside the canonical TESTS vocabulary — " +
                        "update the canonical TESTS contract first");
                }
            }
        }

        violations.Should().BeEmpty(string.Join("; ", violations));
    }

    private static string FindPlanFile()
    {
        var current = AppContext.BaseDirectory;
        while (current is not null)
        {
            var candidate = Path.Combine(
                current,
                "docs", "workstreams", "executions", "identity-accounts", "identity-accounts.plan.md");

            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = Path.GetDirectoryName(current);
        }

        throw new FileNotFoundException(
            "identity-accounts.plan.md not found from test base directory.");
    }

    private static Dictionary<string, (string? Requirements, string? TestFamilies, string? Surface, string? Evidence, bool Duplicate)>
        ExtractMatrixRows(string planPath)
    {
        var lines = File.ReadAllLines(planPath);

        var headingIndex = Array.FindIndex(lines, l => l.Trim() == MatrixHeading);
        headingIndex.Should().BeGreaterThanOrEqualTo(
            0, "canonical PLAN must contain the normative traceability matrix heading");

        var rows = new Dictionary<string, (string?, string?, string?, string?, bool)>();
        var seenCounts = new Dictionary<string, int>();

        for (var i = headingIndex; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith('|'))
            {
                if (rows.Count > 0 && line.StartsWith('#'))
                {
                    break; // next section
                }

                continue;
            }

            var cells = line.Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length < 6 || cells[1].Length == 0)
            {
                continue;
            }

            var unit = cells[1].Trim('`');
            if (!unit.StartsWith("P13-", StringComparison.Ordinal)
                && !unit.StartsWith("Phase ", StringComparison.Ordinal))
            {
                continue;
            }

            seenCounts[unit] = seenCounts.GetValueOrDefault(unit) + 1;
            rows[unit] = (
                Requirements: cells[2],
                TestFamilies: cells[3],
                Surface: cells[4],
                Evidence: cells[5],
                Duplicate: seenCounts[unit] > 1);
        }

        return rows;
    }
}
