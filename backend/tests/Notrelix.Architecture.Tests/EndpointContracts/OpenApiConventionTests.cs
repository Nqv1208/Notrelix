namespace Notrelix.Architecture.Tests.EndpointContracts;

/// <summary>
/// Verifies OpenAPI and endpoint naming conventions are stable for frontend consumption.
/// </summary>
public sealed class OpenApiConventionTests
{
    private static readonly HashSet<string> OperationalEndpoints = new(StringComparer.Ordinal)
    {
        "GetHealth", "LivenessProbe", "ReadinessProbe", "GetOutboxStats",
        "GetPendingOutboxMessages", "GetFailedOutboxMessages", "GetOutboxMessageById"
    };

    [Fact]
    public void All_endpoint_files_must_have_WithName()
    {
        var endpointPath = Path.Combine(FindApiSourcePath(), "Endpoints");

        var endpointFiles = Directory.GetFiles(endpointPath, "*Endpoint*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !Path.GetFileName(f).StartsWith("Map")
                     && !Path.GetFileName(f).Contains("Extensions"))
            .ToArray();

        endpointFiles.Should().NotBeEmpty("API must have endpoint files");

        var violations = new List<string>();

        foreach (var file in endpointFiles)
        {
            var content = File.ReadAllText(file);
            if (content.Contains("MapGet(") || content.Contains("MapPost(") ||
                content.Contains("MapPut(") || content.Contains("MapDelete(") ||
                content.Contains("MapPatch(") || content.Contains("MapMethods("))
            {
                if (!content.Contains(".WithName("))
                {
                    violations.Add(Path.GetRelativePath(endpointPath, file));
                }
            }
        }

        violations.Should().BeEmpty(
            "every endpoint route must have .WithName() for stable operationId. Missing:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void OperationIds_must_be_unique()
    {
        var endpointPath = Path.Combine(FindApiSourcePath(), "Endpoints");

        var files = Directory.GetFiles(endpointPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var operationIds = new Dictionary<string, string>();
        var duplicates = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var matches = System.Text.RegularExpressions.Regex.Matches(
                content, @"\.WithName\(""([^""]+)""\)");

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var name = match.Groups[1].Value;
                var relativePath = Path.GetRelativePath(endpointPath, file);

                if (operationIds.TryGetValue(name, out var existing))
                    duplicates.Add($"'{name}' in both {existing} and {relativePath}");
                else
                    operationIds[name] = relativePath;
            }
        }

        operationIds.Should().NotBeEmpty("endpoints must have operationIds");
        duplicates.Should().BeEmpty(
            "operationIds must be unique across v1. Duplicates:\n" +
            string.Join("\n", duplicates));
    }

    [Fact]
    public void OperationIds_must_follow_dotted_format()
    {
        var endpointPath = Path.Combine(FindApiSourcePath(), "Endpoints");

        var files = Directory.GetFiles(endpointPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var matches = System.Text.RegularExpressions.Regex.Matches(
                content, @"\.WithName\(""([^""]+)""\)");

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var name = match.Groups[1].Value;

                if (OperationalEndpoints.Contains(name))
                    continue;

                if (!name.Contains('.'))
                    violations.Add($"'{name}' must use dotted Context.Action format");
            }
        }

        violations.Should().BeEmpty(
            "operationIds must follow Context.Resource.Action format. Violations:\n" +
            string.Join("\n", violations));
    }

    private static string FindApiSourcePath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.API");
    }
}
