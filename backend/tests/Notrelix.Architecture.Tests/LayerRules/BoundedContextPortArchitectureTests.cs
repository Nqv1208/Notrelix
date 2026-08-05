namespace Notrelix.Architecture.Tests;

public class BoundedContextPortArchitectureTests
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

    private static string[] GetFeatureFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();
    }

    private static readonly HashSet<string> MigratedCommandFiles =
    [
        "CreateWorkspace.cs",
        "ArchiveWorkspace.cs",
        "UpdateWorkspace.cs",
        "RestoreWorkspace.cs",
        "CreateBoardInWorkspace.cs",
        "UpdateBoard.cs",
        "ArchiveBoard.cs",
        "UnarchiveBoard.cs",
    ];

    private static readonly HashSet<string> MigratedQueryFiles =
    [
        "GetBoard.cs",
        "GetBoards.cs",
    ];

    [Fact]
    public void MigratedWorkspaceHandlers_ShouldNotInject_ApplicationDbContext()
    {
        var files = GetFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (!MigratedCommandFiles.Contains(fileName) && !MigratedQueryFiles.Contains(fileName))
                continue;

            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IRequestHandler<")) continue;

            if (content.Contains("ApplicationDbContext"))
            {
                violations.Add(fileName);
            }
        }

        violations.Should().BeEmpty(
            $"Migrated handlers must use bounded-context DbContext interfaces (IWorkspaceDbContext/IWorkManagementDbContext) " +
            $"instead of ApplicationDbContext. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void MigratedWorkspaceHandlers_ShouldUse_CorrectBoundedContext()
    {
        var files = GetFeatureFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (!MigratedCommandFiles.Contains(fileName) && !MigratedQueryFiles.Contains(fileName))
                continue;

            var content = RemoveComments(File.ReadAllText(file));
            if (!content.Contains("IRequestHandler<")) continue;

            var isWorkspace = fileName is "CreateWorkspace.cs" or "ArchiveWorkspace.cs" or "UpdateWorkspace.cs" or "RestoreWorkspace.cs";
            var isBoard = fileName is "CreateBoardInWorkspace.cs" or "UpdateBoard.cs" or "ArchiveBoard.cs" or "UnarchiveBoard.cs" or "GetBoard.cs" or "GetBoards.cs";

            if (isWorkspace && !content.Contains("IWorkspaceDbContext"))
            {
                violations.Add($"{fileName}: should use IWorkspaceDbContext");
            }

            if (isBoard && !content.Contains("IWorkManagementDbContext"))
            {
                violations.Add($"{fileName}: should use IWorkManagementDbContext");
            }
        }

        violations.Should().BeEmpty(
            $"Migrated handlers must inject their bounded-context DbContext. Violations: {string.Join(", ", violations)}");
    }

    private static string RemoveComments(string input)
    {
        var blockComments = @"/\*(.*?)\*/";
        var lineComments = @"//(.*?)\r?\n";
        var cleaned = Regex.Replace(input, blockComments, "", RegexOptions.Singleline);
        cleaned = Regex.Replace(cleaned, lineComments, "\n");
        return cleaned;
    }
}
