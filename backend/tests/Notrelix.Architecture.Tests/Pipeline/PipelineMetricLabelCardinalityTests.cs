namespace Notrelix.Architecture.Tests.Pipeline;

/// <summary>
/// CERT-OBS-001 / NRX-016 — the application pipeline meter must stay
/// low-cardinality. Every label key used with a pipeline instrument has to be a
/// canonical code-bounded category; an identifier-shaped label (user id,
/// tenant id, email, token, slug, free-form message) turns the metric backend
/// into an unbounded-dimension store and is a security problem, not only a cost
/// problem.
///
/// Adding a new code-bounded label is therefore an explicit, reviewed change:
/// it must be added to <see cref="AllowedLabelKeys"/> in the same change.
/// </summary>
public class PipelineMetricLabelCardinalityTests : ArchitectureTestBase
{
    private static readonly string[] AllowedLabelKeys =
    [
        "decision.kind",
        "error.category",
        "stage",
    ];

    private static readonly Regex LabelKeyPattern =
        new(@"KeyValuePair<string,\s*object\?>\(\s*""([^""]+)""", RegexOptions.Compiled);

    [Fact]
    public void Pipeline_metric_labels_are_canonical_code_bounded_categories()
    {
        var offenders = new List<string>();

        foreach (var file in Directory
                     .GetFiles(GetApplicationPath(), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            offenders.AddRange(
                FindNonCanonicalLabels(RemoveComments(File.ReadAllText(file)), Path.GetFileName(file)));
        }

        offenders.Should().BeEmpty(
            "pipeline metric labels must be code-bounded categories drawn from {0}; add a new " +
            "code-bounded label there explicitly rather than passing an identifier or free-form value",
            string.Join(", ", AllowedLabelKeys));
    }

    [Fact]
    public void Scanner_rejects_an_identifier_shaped_label()
    {
        // Proves the gate can actually fail: a violating fixture must not pass.
        const string violatingSource = """
            _metrics.PipelineFailures.Add(1,
                new KeyValuePair<string, object?>("user.id", principalId.ToString()));
            """;

        FindNonCanonicalLabels(violatingSource, "Fixture.cs")
            .Should().ContainSingle()
            .Which.Should().Contain("user.id");
    }

    [Fact]
    public void Scanner_accepts_every_canonical_label()
    {
        var canonical = string.Join(
            "\n",
            AllowedLabelKeys.Select(key => $@"new KeyValuePair<string, object?>(""{key}"", ""x"");"));

        FindNonCanonicalLabels(canonical, "Canonical.cs").Should().BeEmpty();
    }

    private static IEnumerable<string> FindNonCanonicalLabels(string source, string fileName)
    {
        return LabelKeyPattern.Matches(source)
            .Select(match => match.Groups[1].Value)
            .Where(key => !AllowedLabelKeys.Contains(key))
            .Select(key => $"{fileName}: pipeline metric label '{key}' is not a canonical code-bounded label");
    }
}