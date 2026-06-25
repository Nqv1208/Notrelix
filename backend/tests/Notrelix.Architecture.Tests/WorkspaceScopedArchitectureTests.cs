namespace Notrelix.Architecture.Tests;

public class WorkspaceScopedArchitectureTests
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

    private static string GetInfrastructurePath()
    {
        var current = AppContext.BaseDirectory;
        while (current != null && !File.Exists(Path.Combine(current, "backend.slnx")))
        {
            current = Path.GetDirectoryName(current);
        }
        if (current == null)
            throw new DirectoryNotFoundException("Could not find backend.slnx root.");
        return Path.Combine(current, "src", "Notrelix.Infrastructure");
    }

    private static string[] GetQueryFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && f.Contains($"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}")
                     && !f.EndsWith("Handler.cs")
                     && !f.EndsWith("Validator.cs")
                     && !f.EndsWith("Result.cs"))
            .ToArray();
    }

    private static readonly HashSet<string> KnownMissingWorkspaceQueryRequest =
    [
        "GetMyBoardItemsQuery", "GetWorkspaceQuery", "GetWorkspaceBySlugQuery",
        "GetWorkspaceMembersBySlugQuery", "GetWorkspaceMembersQuery",
        "GetWorkspaceActivityBySlugQuery", "GetWorkspaceActivityQuery",
        "GetWorkspaceInvitationsQuery", "GetResourcePermissionsQuery",
        "GetWorkspaceAutomationsQuery", "GetWorkspacePagesQuery",
        "SearchPagesQuery", "GetPageTreeQuery",
    ];

    [Fact]
    public void QueryRecords_WithWorkspaceId_ShouldImplement_IWorkspaceRequest()
    {
        var files = GetQueryFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            var hasWorkspaceId = content.Contains("Guid WorkspaceId") || content.Contains("Guid? WorkspaceId");
            if (!hasWorkspaceId) continue;

            if (!declaration.Contains("IWorkspaceRequest"))
            {
                if (!KnownMissingWorkspaceQueryRequest.Contains(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty($"Query records with WorkspaceId must implement IWorkspaceRequest. Fix known violations by removing from KnownMissingWorkspaceQueryRequest: {string.Join(", ", violations)}");
    }

    [Fact]
    public void QueryRecords_ImplementingIWorkspaceRequest_ShouldAlsoHaveWorkspaceId()
    {
        var files = GetQueryFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;
            if (!declaration.Contains("IWorkspaceRequest")) continue;

            var hasWorkspaceId = content.Contains("Guid WorkspaceId") || content.Contains("Guid? WorkspaceId");
            if (!hasWorkspaceId)
                violations.Add($"{name}: {Path.GetFileName(file)} has IWorkspaceRequest but no WorkspaceId property");
        }

        violations.Should().BeEmpty($"Records implementing IWorkspaceRequest must have a WorkspaceId property: {string.Join(", ", violations)}");
    }

    [Fact]
    public void QueryRecords_WithWorkspaceRequest_ShouldAlsoImplement_IQuery()
    {
        var files = GetQueryFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (!declaration.Contains("IWorkspaceRequest")) continue;
            if (declaration.Contains("IQuery") || declaration.Contains("ICommand")) continue;

            violations.Add($"{name}: {Path.GetFileName(file)} implements IWorkspaceRequest but not IQuery/ICommand");
        }

        violations.Should().BeEmpty($"Records with IWorkspaceRequest must also implement IQuery or ICommand: {string.Join(", ", violations)}");
    }

    private static string ReadDeclaration(string content)
    {
        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("public record") && !trimmed.StartsWith("public sealed record"))
                continue;

            var declaration = trimmed;
            var parenDepth = trimmed.Count(c => c == '(') - trimmed.Count(c => c == ')');

            if (parenDepth != 0 || (!trimmed.Contains(';') && !trimmed.Contains('{') && !trimmed.Contains(':')))
            {
                for (var j = i + 1; j < lines.Length && parenDepth >= 0; j++)
                {
                    var nextLine = lines[j].Trim();
                    declaration += " " + nextLine;
                    parenDepth += nextLine.Count(c => c == '(') - nextLine.Count(c => c == ')');
                    if (parenDepth <= 0 && (nextLine.Contains(';') || nextLine.Contains('{') || nextLine.Contains(':')))
                        break;
                }
            }
            return declaration;
        }
        return string.Empty;
    }

    private static string ExtractRecordName(string declaration)
    {
        var match = Regex.Match(declaration, @"public\s+(?:sealed\s+)?record\s+(\w+)");
        return match.Success ? match.Groups[1].Value : string.Empty;
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
