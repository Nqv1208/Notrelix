namespace Notrelix.Architecture.Tests.ApplicationLayer;

/// <summary>
/// APP-PATH-001..004: Enforces canonical module-first Application layout.
/// Canonical: Features/{Context}/{Module}/Commands|Queries/{UseCase}/
/// Forbidden: Features/{Context}/Commands|Queries/{Module}/ (legacy)
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
                p is "Commands" or "Queries" or "Abstractions" or "Services");

            if (!hasCommandsOrQueries)
            {
                violations.Add(relativePath);
            }
        }

        violations.Should().BeEmpty(
            "all handler files must be under Commands/ or Queries/ subdirectories");
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
