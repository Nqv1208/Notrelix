namespace Notrelix.Architecture.Tests;

/// <summary>
/// Architecture tests enforcing workspace namespace rules:
/// - Commands under WorkManagement, Documents, Collaboration must implement IWorkspaceRequest.
/// - Workspace-scoped commands must implement IRequirePermission.
/// </summary>
public class WorkspaceNamespaceArchitectureTests
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

    private static string[] GetCommandFiles(string featurePath)
    {
        var appPath = GetApplicationPath();
        var fullPath = Path.Combine(appPath, featurePath);
        if (!Directory.Exists(fullPath))
            return [];

        return Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && f.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}")
                     && !f.EndsWith("Handler.cs")
                     && !f.EndsWith("Validator.cs")
                     && !f.EndsWith("Result.cs"))
            .ToArray();
    }

    private static string[] GetQueryFiles(string featurePath)
    {
        var appPath = GetApplicationPath();
        var fullPath = Path.Combine(appPath, featurePath);
        if (!Directory.Exists(fullPath))
            return [];

        return Directory.GetFiles(fullPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && f.Contains($"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}")
                     && !f.EndsWith("Handler.cs")
                     && !f.EndsWith("Validator.cs")
                     && !f.EndsWith("Result.cs"))
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

    private static string ReadDeclaration(string content)
    {
        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("public record") && !trimmed.StartsWith("public sealed record"))
                continue;

            var declaration = trimmed;

            if (trimmed.Contains(';') || trimmed.Contains('{'))
                return declaration;

            for (var j = i + 1; j < lines.Length; j++)
            {
                var nextLine = lines[j].Trim();
                declaration += " " + nextLine;

                if (nextLine.Contains(';') || nextLine.Contains('{'))
                    break;
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

    // --- Allowlists for commands missing IWorkspaceRequest ---

    private static readonly Dictionary<string, AllowlistEntry> WorkManagementMissingWorkspaceRequest = new()
    {
        ["CreateBoardItemLinkCommand"] = new("CreateBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "ItemLinks command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
        ["DeleteBoardItemLinkCommand"] = new("DeleteBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "ItemLinks command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
        ["CreateBoardBySlugCommand"] = new("CreateBoardBySlugCommand", AllowlistClassification.LegacyGap,
            "Board command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
        ["SetBoardItemDueDateCommand"] = new("SetBoardItemDueDateCommand", AllowlistClassification.LegacyGap,
            "BoardItem command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
        ["UpdateBoardItemStatusCommand"] = new("UpdateBoardItemStatusCommand", AllowlistClassification.LegacyGap,
            "BoardItem command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
    };

    private static readonly Dictionary<string, AllowlistEntry> DocumentsMissingWorkspaceRequest = new()
    {
        ["PublishPageCommand"] = new("PublishPageCommand", AllowlistClassification.LegacyGap,
            "Page command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.LegacyGap,
            "Page command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
        ["CreatePageCommand"] = new("CreatePageCommand", AllowlistClassification.LegacyGap,
            "Page command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
        ["SetPageDeadlineCommand"] = new("SetPageDeadlineCommand", AllowlistClassification.LegacyGap,
            "Page command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
        ["MovePageCommand"] = new("MovePageCommand", AllowlistClassification.LegacyGap,
            "Page command — has neither IWorkspaceRequest nor IResourceScopedRequest", "Add IResourceScopedRequest"),
    };

    private static readonly Dictionary<string, AllowlistEntry> CollaborationMissingWorkspaceRequest = new()
    {
    };

    // --- Allowlists for commands missing IRequirePermission ---

    private static readonly Dictionary<string, AllowlistEntry> WorkManagementMissingPermission = new()
    {
        ["CreateBoardItemLinkCommand"] = new("CreateBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "ItemLinks command — missing IRequirePermission", "Add IRequirePermission"),
        ["DeleteBoardItemLinkCommand"] = new("DeleteBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "ItemLinks command — missing IRequirePermission", "Add IRequirePermission"),
        ["CreateBoardBySlugCommand"] = new("CreateBoardBySlugCommand", AllowlistClassification.LegacyGap,
            "Board command — missing IRequirePermission", "Add IRequirePermission"),
        ["SetBoardItemDueDateCommand"] = new("SetBoardItemDueDateCommand", AllowlistClassification.LegacyGap,
            "BoardItem command — missing IRequirePermission", "Add IRequirePermission"),
        ["UpdateBoardItemStatusCommand"] = new("UpdateBoardItemStatusCommand", AllowlistClassification.LegacyGap,
            "BoardItem command — missing IRequirePermission", "Add IRequirePermission"),
    };

    private static readonly Dictionary<string, AllowlistEntry> DocumentsMissingPermission = new()
    {
        ["PublishPageCommand"] = new("PublishPageCommand", AllowlistClassification.LegacyGap,
            "Page command — missing IRequirePermission", "Add IRequirePermission"),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.LegacyGap,
            "Page command — missing IRequirePermission", "Add IRequirePermission"),
        ["CreatePageCommand"] = new("CreatePageCommand", AllowlistClassification.LegacyGap,
            "Page command — missing IRequirePermission", "Add IRequirePermission"),
        ["SetPageDeadlineCommand"] = new("SetPageDeadlineCommand", AllowlistClassification.LegacyGap,
            "Page command — missing IRequirePermission", "Add IRequirePermission"),
        ["MovePageCommand"] = new("MovePageCommand", AllowlistClassification.LegacyGap,
            "Page command — missing IRequirePermission", "Add IRequirePermission"),
    };

    private static readonly Dictionary<string, AllowlistEntry> CollaborationMissingPermission = new()
    {
    };

    // --- Allowlists for workspace commands ---

    private static readonly Dictionary<string, AllowlistEntry> WorkspaceCommandsMissingClassification = new()
    {
        ["AcceptInvitationCommand"] = new("AcceptInvitationCommand", AllowlistClassification.PublicCommand,
            "Token-scoped invitation command — auth required, no resource scope", "Keep as-is"),
        ["ProvisionPersonalWorkspaceCommand"] = new("ProvisionPersonalWorkspaceCommand", AllowlistClassification.SystemCommand,
            "System-internal background command — no user request path", "Keep as-is"),
    };

    // --- Validation tests ---

    [Fact]
    public void Allowlists_ShouldHaveNoDuplicateEntries()
    {
        var allAllowlists = new Dictionary<string, Dictionary<string, AllowlistEntry>>
        {
            ["WorkManagement_MissingWorkspaceRequest"] = WorkManagementMissingWorkspaceRequest,
            ["Documents_MissingWorkspaceRequest"] = DocumentsMissingWorkspaceRequest,
            ["Collaboration_MissingWorkspaceRequest"] = CollaborationMissingWorkspaceRequest,
            ["WorkManagement_MissingPermission"] = WorkManagementMissingPermission,
            ["Documents_MissingPermission"] = DocumentsMissingPermission,
            ["Collaboration_MissingPermission"] = CollaborationMissingPermission,
            ["WorkspaceCommands_MissingClassification"] = WorkspaceCommandsMissingClassification,
        };

        var violations = new List<string>();

        foreach (var (listName, allowlist) in allAllowlists)
        {
            var duplicates = allowlist.Keys
                .GroupBy(k => k)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var dup in duplicates)
                violations.Add($"{listName}: duplicate entry '{dup}'");
        }

        violations.Should().BeEmpty(
            $"Allowlists must not contain duplicate entries. Violations: {string.Join(", ", violations)}");
    }

    // --- Namespace enforcement tests ---

    [Fact]
    public void WorkManagementCommands_ShouldImplement_IWorkspaceRequest()
    {
        var files = GetCommandFiles("Features/WorkManagement");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (!declaration.Contains("IWorkspaceRequest") && !declaration.Contains("IResourceScopedRequest"))
            {
                if (!WorkManagementMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"WorkManagement commands must implement IWorkspaceRequest or IResourceScopedRequest. " +
            $"Fix by adding to WorkManagementMissingWorkspaceRequest with classification, or add IWorkspaceRequest/IResourceScopedRequest. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DocumentsCommands_ShouldImplement_IWorkspaceRequest()
    {
        var files = GetCommandFiles("Features/Documents");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (!declaration.Contains("IWorkspaceRequest") && !declaration.Contains("IResourceScopedRequest"))
            {
                if (!DocumentsMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Documents commands must implement IWorkspaceRequest or IResourceScopedRequest. " +
            $"Fix by adding to DocumentsMissingWorkspaceRequest with classification, or add IWorkspaceRequest/IResourceScopedRequest. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CollaborationCommands_ShouldImplement_IWorkspaceRequest()
    {
        var files = GetCommandFiles("Features/Collaboration");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (!declaration.Contains("IWorkspaceRequest") && !declaration.Contains("IResourceScopedRequest"))
            {
                if (!CollaborationMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Collaboration commands must implement IWorkspaceRequest or IResourceScopedRequest. " +
            $"Fix by adding to CollaborationMissingWorkspaceRequest with classification, or add IWorkspaceRequest/IResourceScopedRequest. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void WorkManagementCommands_ShouldImplement_IRequirePermission()
    {
        var files = GetCommandFiles("Features/WorkManagement");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (!declaration.Contains("IRequirePermission"))
            {
                if (!WorkManagementMissingPermission.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"WorkManagement commands must implement IRequirePermission. " +
            $"Fix by adding to WorkManagementMissingPermission with classification, or add IRequirePermission. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DocumentsCommands_ShouldImplement_IRequirePermission()
    {
        var files = GetCommandFiles("Features/Documents");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (!declaration.Contains("IRequirePermission"))
            {
                if (!DocumentsMissingPermission.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Documents commands must implement IRequirePermission. " +
            $"Fix by adding to DocumentsMissingPermission with classification, or add IRequirePermission. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CollaborationCommands_ShouldImplement_IRequirePermission()
    {
        var files = GetCommandFiles("Features/Collaboration");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (!declaration.Contains("IRequirePermission"))
            {
                if (!CollaborationMissingPermission.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Collaboration commands must implement IRequirePermission. " +
            $"Fix by adding to CollaborationMissingPermission with classification, or add IRequirePermission. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CommandsImplementingWorkspaceRequest_ShouldAlsoImplement_IRequirePermission()
    {
        var featurePaths = new[] { "Features/WorkManagement", "Features/Documents", "Features/Collaboration", "Features/Workspaces" };
        var violations = new List<string>();

        foreach (var featurePath in featurePaths)
        {
            var files = GetCommandFiles(featurePath);
            foreach (var file in files)
            {
                var content = RemoveComments(File.ReadAllText(file));
                var declaration = ReadDeclaration(content);
                if (string.IsNullOrEmpty(declaration)) continue;

                var name = ExtractRecordName(declaration);
                if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

                var hasWorkspaceMarker = declaration.Contains("IWorkspaceRequest") || declaration.Contains("IResourceScopedRequest");
                if (hasWorkspaceMarker && !declaration.Contains("IRequirePermission"))
                {
                    violations.Add($"{name}: {Path.GetFileName(file)} implements IWorkspaceRequest/IResourceScopedRequest but not IRequirePermission");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Commands implementing IWorkspaceRequest or IResourceScopedRequest must also implement IRequirePermission. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void QueriesImplementingWorkspaceRequest_ShouldAlsoImplement_IRequirePermission()
    {
        var featurePaths = new[] { "Features/WorkManagement", "Features/Documents", "Features/Collaboration", "Features/Workspaces" };
        var violations = new List<string>();

        foreach (var featurePath in featurePaths)
        {
            var files = GetQueryFiles(featurePath);
            foreach (var file in files)
            {
                var content = RemoveComments(File.ReadAllText(file));
                var declaration = ReadDeclaration(content);
                if (string.IsNullOrEmpty(declaration)) continue;

                var name = ExtractRecordName(declaration);
                if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

                var hasWorkspaceMarker = declaration.Contains("IWorkspaceRequest") || declaration.Contains("IResourceScopedRequest");
                if (hasWorkspaceMarker && !declaration.Contains("IRequirePermission"))
                {
                    violations.Add($"{name}: {Path.GetFileName(file)} implements IWorkspaceRequest/IResourceScopedRequest but not IRequirePermission");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Queries implementing IWorkspaceRequest or IResourceScopedRequest must also implement IRequirePermission. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void WorkspaceCommands_ShouldImplement_IWorkspaceRequest_Or_IAccountRequest()
    {
        var files = GetCommandFiles("Features/Workspaces");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (!declaration.Contains("IWorkspaceRequest") && !declaration.Contains("IAccountRequest"))
            {
                if (!WorkspaceCommandsMissingClassification.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Workspace commands must implement IWorkspaceRequest or IAccountRequest. " +
            $"Fix by adding to WorkspaceCommandsMissingClassification with classification, or add IWorkspaceRequest/IAccountRequest. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void NoSlugBasedWorkspaceCommands_ShouldExist()
    {
        var files = GetCommandFiles("Features/Workspaces");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Contains("BySlug"))
                violations.Add($"{fileName}");
        }

        violations.Should().BeEmpty(
            $"No slug-based workspace commands may exist. All BySlug commands have been deleted. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void NoSlugBasedWorkspaceQueries_ShouldExist()
    {
        var files = GetQueryFiles("Features/Workspaces");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Contains("BySlug"))
                violations.Add($"{fileName}");
        }

        violations.Should().BeEmpty(
            $"No slug-based workspace queries may exist. All BySlug queries have been deleted. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void WorkspaceCommands_ShouldNotIntroduceNewSlugBasedCommands()
    {
        var files = GetCommandFiles("Features/Workspaces");
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.Contains("BySlug"))
                violations.Add($"{name}: {Path.GetFileName(file)}");
        }

        violations.Should().BeEmpty(
            $"New slug-based workspace commands are forbidden. Use workspaceId from route/context instead. " +
            $"Violations: {string.Join(", ", violations)}");
    }

}
