namespace Notrelix.Architecture.Tests;

public class DbContextBoundaryArchitectureTests
{
    private static string GetApplicationPath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.Application");
    }

    private static string[] GetHandlerFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && f.Contains("Handler.cs"))
            .ToArray();
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }

    private static readonly Dictionary<string, string> ModuleToExpectedContext = new()
    {
        ["Workspaces"] = "IWorkspaceDbContext",
        ["WorkManagement"] = "IWorkManagementDbContext",
        ["Identity"] = "IIdentityDbContext",
    };

    private static readonly HashSet<string> KnownCrossModuleViolations = [];

    [Fact]
    public void WorkspaceHandlers_ShouldNotInject_WorkManagementDbContext()
    {
        var violations = AssertModuleContextBoundaries("Workspaces", "IWorkManagementDbContext");
        violations.Should().BeEmpty(
            $"Workspace handlers should not inject IWorkManagementDbContext. " +
            $"Use IWorkspaceDbContext instead. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void WorkManagementHandlers_ShouldNotInject_WorkspaceDbContext()
    {
        var violations = AssertModuleContextBoundaries("WorkManagement", "IWorkspaceDbContext");
        violations.Should().BeEmpty(
            $"WorkManagement handlers should not inject IWorkspaceDbContext. " +
            $"Use IWorkManagementDbContext instead. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void IdentityHandlers_ShouldNotInject_WorkspaceDbContext()
    {
        var violations = AssertModuleContextBoundaries("Identity", "IWorkspaceDbContext");
        violations.Should().BeEmpty(
            $"Identity handlers should not inject IWorkspaceDbContext. " +
            $"Use IIdentityDbContext instead. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void IdentityHandlers_ShouldNotInject_WorkManagementDbContext()
    {
        var violations = AssertModuleContextBoundaries("Identity", "IWorkManagementDbContext");
        violations.Should().BeEmpty(
            $"Identity handlers should not inject IWorkManagementDbContext. " +
            $"Use IIdentityDbContext instead. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void WorkManagementHandlers_ShouldNotInject_IdentityDbContext()
    {
        var violations = AssertModuleContextBoundaries("WorkManagement", "IIdentityDbContext");
        violations.Should().BeEmpty(
            $"WorkManagement handlers should not inject IIdentityDbContext. " +
            $"Use IWorkManagementDbContext instead. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void WorkspaceHandlers_ShouldNotInject_IdentityDbContext()
    {
        var violations = AssertModuleContextBoundaries("Workspaces", "IIdentityDbContext");
        violations.Should().BeEmpty(
            $"Workspace handlers should not inject IIdentityDbContext. " +
            $"Use IWorkspaceDbContext instead. Violations: {string.Join(", ", violations)}");
    }

    private static List<string> AssertModuleContextBoundaries(string module, string forbiddenContext)
    {
        var files = GetHandlerFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            if (!file.Contains($"{Path.DirectorySeparatorChar}{module}{Path.DirectorySeparatorChar}"))
                continue;

            var content = RemoveComments(File.ReadAllText(file));
            var fileName = Path.GetFileName(file);

            if (KnownCrossModuleViolations.Contains(fileName))
                continue;

            if (content.Contains(forbiddenContext))
            {
                violations.Add($"{fileName} injects {forbiddenContext}");
            }
        }

        return violations;
    }
}
