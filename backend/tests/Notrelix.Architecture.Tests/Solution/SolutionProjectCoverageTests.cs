using System.Xml.Linq;

namespace Notrelix.Architecture.Tests.Solution;

/// <summary>
/// Guards the normal build/test graph declared by backend/backend.slnx.
/// A project checked into backend/src or backend/tests that is omitted from
/// the solution silently escapes CI build and test coverage.
/// </summary>
public class SolutionProjectCoverageTests
{
    private const string SolutionFileName = "backend.slnx";
    private const char AltDirectorySeparator = '/';

    /// <summary>
    /// Testing helper projects intentionally excluded from the coverage assertion.
    /// Every entry is a named constant with a reason. Currently none are excluded:
    /// Notrelix.Testing.* helpers are first-class solution members.
    /// </summary>
    private static readonly IReadOnlySet<string> IntentionallyExcludedProjects =
        new HashSet<string>(StringComparer.Ordinal)
        {
        };

    [Fact]
    public void Every_Backend_Project_Is_In_The_Solution()
    {
        var backendRoot = FindBackendRoot();
        var solutionProjects = ReadSolutionProjects(backendRoot);

        var onDiskProjects = EnumerateBackendProjects(backendRoot);

        var missing = onDiskProjects
            .Where(path => !IntentionallyExcludedProjects.Contains(path))
            .Where(path => !solutionProjects.Contains(path, StringComparer.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        missing.Should().BeEmpty(
            "every backend/src and backend/tests project must be present in {0} so CI " +
            "build/test cannot silently omit it",
            SolutionFileName);
    }

    [Fact]
    public void Platform_And_Platform_Tests_Build_Through_The_Solution()
    {
        var backendRoot = FindBackendRoot();
        var solutionProjects = ReadSolutionProjects(backendRoot);

        solutionProjects.Should().Contain("src/Notrelix.Platform/Notrelix.Platform.csproj",
            "Platform is a production dependency graph member and must build via backend.slnx");
        solutionProjects.Should().Contain("tests/Notrelix.Platform.Tests/Notrelix.Platform.Tests.csproj",
            "Platform.Tests is a production test member and must run via backend.slnx");
    }

    [Fact]
    public void Solution_Projects_Exist_On_Disk()
    {
        var backendRoot = FindBackendRoot();

        foreach (var solutionProject in ReadSolutionProjects(backendRoot))
        {
            var fullPath = Path.Combine(backendRoot, solutionProject.Replace(AltDirectorySeparator, Path.DirectorySeparatorChar));
            File.Exists(fullPath).Should().BeTrue(
                $"solution entry '{solutionProject}' must resolve to an existing project file");
        }
    }

    private static string FindBackendRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFileName)))
                return current.FullName;

            current = current.Parent;
        }

        throw new Xunit.Sdk.XunitException(
            $"Could not locate '{SolutionFileName}' walking up from '{AppContext.BaseDirectory}'.");
    }

    private static string[] ReadSolutionProjects(string backendRoot)
    {
        var solutionPath = Path.Combine(backendRoot, SolutionFileName);
        var document = XDocument.Load(solutionPath);

        return document
            .Descendants("Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => NormalizePath(path!))
            .ToArray();
    }

    private static string[] EnumerateBackendProjects(string backendRoot)
    {
        return new[] { "src", "tests" }
            .SelectMany(directory => Directory.EnumerateFiles(
                Path.Combine(backendRoot, directory),
                "*.csproj",
                SearchOption.AllDirectories))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("obj"))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Contains("bin"))
            .Select(path => NormalizePath(Path.GetRelativePath(backendRoot, path)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, AltDirectorySeparator);
    }
}
