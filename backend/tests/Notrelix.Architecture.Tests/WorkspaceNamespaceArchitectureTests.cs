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

    // --- Allowlists for commands missing IWorkspaceRequest ---

    private static readonly Dictionary<string, AllowlistEntry> WorkManagementMissingWorkspaceRequest = new()
    {
        ["CreateBoardFieldCommand"] = new("CreateBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateBoardFieldCommand"] = new("UpdateBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["ReorderBoardFieldsCommand"] = new("ReorderBoardFieldsCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteBoardFieldCommand"] = new("DeleteBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreateBoardItemLinkCommand"] = new("CreateBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteBoardItemLinkCommand"] = new("DeleteBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["AddLabelToBoardItemCommand"] = new("AddLabelToBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["RemoveLabelFromBoardItemCommand"] = new("RemoveLabelFromBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteLabelCommand"] = new("DeleteLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateLabelCommand"] = new("UpdateLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreateLabelCommand"] = new("CreateLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["RemoveBoardMemberCommand"] = new("RemoveBoardMemberCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["AddBoardMemberCommand"] = new("AddBoardMemberCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["ArchiveBoardCommand"] = new("ArchiveBoardCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UnarchiveBoardCommand"] = new("UnarchiveBoardCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreateBoardBySlugCommand"] = new("CreateBoardBySlugCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateBoardItemFieldValuesCommand"] = new("UpdateBoardItemFieldValuesCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UnassignBoardItemMemberCommand"] = new("UnassignBoardItemMemberCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["SetBoardItemDueDateCommand"] = new("SetBoardItemDueDateCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateBoardItemStatusCommand"] = new("UpdateBoardItemStatusCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["LinkPageToBoardItemCommand"] = new("LinkPageToBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["ArchiveBoardItemCommand"] = new("ArchiveBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UnlinkPageFromBoardItemCommand"] = new("UnlinkPageFromBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateBoardItemCommand"] = new("UpdateBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["DuplicateBoardItemCommand"] = new("DuplicateBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["ArchiveBoardGroupCommand"] = new("ArchiveBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreateBoardGroupCommand"] = new("CreateBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateBoardGroupCommand"] = new("UpdateBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["ReorderBoardGroupsCommand"] = new("ReorderBoardGroupsCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["DuplicateBoardGroupCommand"] = new("DuplicateBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UnarchiveBoardGroupCommand"] = new("UnarchiveBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreateChecklistCommand"] = new("CreateChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateChecklistCommand"] = new("UpdateChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteChecklistCommand"] = new("DeleteChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreateChecklistItemCommand"] = new("CreateChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateChecklistItemCommand"] = new("UpdateChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["ToggleChecklistItemCommand"] = new("ToggleChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteChecklistItemCommand"] = new("DeleteChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteBoardViewCommand"] = new("DeleteBoardViewCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
    };

    private static readonly Dictionary<string, AllowlistEntry> DocumentsMissingWorkspaceRequest = new()
    {
        ["DeletePageCommand"] = new("DeletePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["PublishPageCommand"] = new("PublishPageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdatePageCommand"] = new("UpdatePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreatePageCommand"] = new("CreatePageCommand", AllowlistClassification.LegacyGap,
            "Documents command has WorkspaceId param but does not implement IWorkspaceRequest", "Add IWorkspaceRequest"),
        ["SetPageDeadlineCommand"] = new("SetPageDeadlineCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["MovePageCommand"] = new("MovePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateBlockCommand"] = new("UpdateBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["BatchUpdateBlocksCommand"] = new("BatchUpdateBlocksCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["ReorderBlocksCommand"] = new("ReorderBlocksCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreateBlockCommand"] = new("CreateBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteBlockCommand"] = new("DeleteBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing workspace marker", "Add IWorkspaceRequest"),
    };

    private static readonly Dictionary<string, AllowlistEntry> CollaborationMissingWorkspaceRequest = new()
    {
        ["CreateBoardItemAttachmentCommand"] = new("CreateBoardItemAttachmentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing workspace marker", "Add IWorkspaceRequest"),
        ["MarkAllNotificationsAsReadCommand"] = new("MarkAllNotificationsAsReadCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing workspace marker", "Add IWorkspaceRequest"),
        ["MarkNotificationAsReadCommand"] = new("MarkNotificationAsReadCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteCommentCommand"] = new("DeleteCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing workspace marker", "Add IWorkspaceRequest"),
        ["UpdateCommentCommand"] = new("UpdateCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing workspace marker", "Add IWorkspaceRequest"),
        ["CreateCommentCommand"] = new("CreateCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing workspace marker", "Add IWorkspaceRequest"),
        ["ResolveCommentCommand"] = new("ResolveCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing workspace marker", "Add IWorkspaceRequest"),
        ["DeleteAttachmentCommand"] = new("DeleteAttachmentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing workspace marker", "Add IWorkspaceRequest"),
    };

    // --- Allowlists for commands missing IRequirePermission ---

    private static readonly Dictionary<string, AllowlistEntry> WorkManagementMissingPermission = new()
    {
        ["CreateBoardFieldCommand"] = new("CreateBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardFieldCommand"] = new("UpdateBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ReorderBoardFieldsCommand"] = new("ReorderBoardFieldsCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteBoardFieldCommand"] = new("DeleteBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateBoardItemLinkCommand"] = new("CreateBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteBoardItemLinkCommand"] = new("DeleteBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["AddLabelToBoardItemCommand"] = new("AddLabelToBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["RemoveLabelFromBoardItemCommand"] = new("RemoveLabelFromBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteLabelCommand"] = new("DeleteLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateLabelCommand"] = new("UpdateLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateLabelCommand"] = new("CreateLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["RemoveBoardMemberCommand"] = new("RemoveBoardMemberCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["AddBoardMemberCommand"] = new("AddBoardMemberCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ArchiveBoardCommand"] = new("ArchiveBoardCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UnarchiveBoardCommand"] = new("UnarchiveBoardCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateBoardBySlugCommand"] = new("CreateBoardBySlugCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardItemFieldValuesCommand"] = new("UpdateBoardItemFieldValuesCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UnassignBoardItemMemberCommand"] = new("UnassignBoardItemMemberCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["SetBoardItemDueDateCommand"] = new("SetBoardItemDueDateCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardItemStatusCommand"] = new("UpdateBoardItemStatusCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["LinkPageToBoardItemCommand"] = new("LinkPageToBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ArchiveBoardItemCommand"] = new("ArchiveBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UnlinkPageFromBoardItemCommand"] = new("UnlinkPageFromBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardItemCommand"] = new("UpdateBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DuplicateBoardItemCommand"] = new("DuplicateBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ArchiveBoardGroupCommand"] = new("ArchiveBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateBoardGroupCommand"] = new("CreateBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardGroupCommand"] = new("UpdateBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ReorderBoardGroupsCommand"] = new("ReorderBoardGroupsCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DuplicateBoardGroupCommand"] = new("DuplicateBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UnarchiveBoardGroupCommand"] = new("UnarchiveBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateChecklistCommand"] = new("CreateChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateChecklistCommand"] = new("UpdateChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteChecklistCommand"] = new("DeleteChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateChecklistItemCommand"] = new("CreateChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateChecklistItemCommand"] = new("UpdateChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ToggleChecklistItemCommand"] = new("ToggleChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteChecklistItemCommand"] = new("DeleteChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteBoardViewCommand"] = new("DeleteBoardViewCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
    };

    private static readonly Dictionary<string, AllowlistEntry> DocumentsMissingPermission = new()
    {
        ["DeletePageCommand"] = new("DeletePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["PublishPageCommand"] = new("PublishPageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["UpdatePageCommand"] = new("UpdatePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["CreatePageCommand"] = new("CreatePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["SetPageDeadlineCommand"] = new("SetPageDeadlineCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["MovePageCommand"] = new("MovePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["UpdateBlockCommand"] = new("UpdateBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["BatchUpdateBlocksCommand"] = new("BatchUpdateBlocksCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["ReorderBlocksCommand"] = new("ReorderBlocksCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["CreateBlockCommand"] = new("CreateBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["DeleteBlockCommand"] = new("DeleteBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
    };

    private static readonly Dictionary<string, AllowlistEntry> CollaborationMissingPermission = new()
    {
        ["CreateBoardItemAttachmentCommand"] = new("CreateBoardItemAttachmentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["MarkAllNotificationsAsReadCommand"] = new("MarkAllNotificationsAsReadCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["MarkNotificationAsReadCommand"] = new("MarkNotificationAsReadCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["DeleteCommentCommand"] = new("DeleteCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["UpdateCommentCommand"] = new("UpdateCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["CreateCommentCommand"] = new("CreateCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["ResolveCommentCommand"] = new("ResolveCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["DeleteAttachmentCommand"] = new("DeleteAttachmentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
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

            if (!declaration.Contains("IWorkspaceRequest"))
            {
                if (!WorkManagementMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"WorkManagement commands must implement IWorkspaceRequest. " +
            $"Fix by adding to WorkManagementMissingWorkspaceRequest with classification, or add IWorkspaceRequest. " +
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

            if (!declaration.Contains("IWorkspaceRequest"))
            {
                if (!DocumentsMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Documents commands must implement IWorkspaceRequest. " +
            $"Fix by adding to DocumentsMissingWorkspaceRequest with classification, or add IWorkspaceRequest. " +
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

            if (!declaration.Contains("IWorkspaceRequest"))
            {
                if (!CollaborationMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Collaboration commands must implement IWorkspaceRequest. " +
            $"Fix by adding to CollaborationMissingWorkspaceRequest with classification, or add IWorkspaceRequest. " +
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
    public void CommandsImplementingIWorkspaceRequest_ShouldAlsoImplement_IRequirePermission()
    {
        var featurePaths = new[] { "Features/WorkManagement", "Features/Documents", "Features/Collaboration" };
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

                if (declaration.Contains("IWorkspaceRequest") && !declaration.Contains("IRequirePermission"))
                {
                    violations.Add($"{name}: {Path.GetFileName(file)} implements IWorkspaceRequest but not IRequirePermission");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Commands implementing IWorkspaceRequest must also implement IRequirePermission. " +
            $"Violations: {string.Join(", ", violations)}");
    }
}
