namespace Notrelix.Architecture.Tests;

public class UseCaseSecurityClassificationTests
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

    private static string[] GetRequestFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.EndsWith("Handler.cs")
                     && !f.EndsWith("Validator.cs")
                     && !f.EndsWith("Result.cs")
                     && !f.EndsWith("Mapper.cs")
                     && !f.EndsWith("Authorization.cs"))
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
                    if (parenDepth <= 0 && (nextLine.Contains(';') || nextLine.Contains('{')))
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

    private const string LegacyGapReason = "Pre-hardening command without security classification marker";
    private const string LegacyGapTarget = "Implement IWorkspaceRequest or IAccountRequest";

    // --- Security classification interfaces ---
    private static readonly string[] SecurityInterfaces =
    [
        "IAnonymousRequest",
        "IAuthenticatedRequest",
        "ISystemInternalRequest",
        "IWorkspaceRequest",
        "IAccountRequest"
    ];

    /// <summary>
    /// Commands and queries that are intentionally unclassified (public, system, identity-based)
    /// or are pre-hardening legacy gaps that don't carry WorkspaceId directly.
    /// </summary>
    private static readonly Dictionary<string, AllowlistEntry> KnownUnclassified = new()
    {
        // === Public auth commands ===
        ["LoginCommand"] = new("LoginCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest"),
        ["LogoutCommand"] = new("LogoutCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest"),
        ["RefreshTokenCommand"] = new("RefreshTokenCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest"),
        ["ForgotPasswordCommand"] = new("ForgotPasswordCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest"),
        ["ResetPasswordCommand"] = new("ResetPasswordCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest"),
        ["RegisterCommand"] = new("RegisterCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest"),

        // === System commands (no workspace/user scope) ===
        ["SendWelcomeEmailCommand"] = new("SendWelcomeEmailCommand", AllowlistClassification.SystemCommand,
            "System command triggered by user registration", "Keep as-is"),
        ["ProvisionPersonalWorkspaceCommand"] = new("ProvisionPersonalWorkspaceCommand", AllowlistClassification.SystemCommand,
            "System command triggered by user registration", "Keep as-is"),
        ["HandleCalendarWebhookCommand"] = new("HandleCalendarWebhookCommand", AllowlistClassification.SystemCommand,
            "External webhook handler", "Keep as-is"),
        ["HandleN8nCallbackCommand"] = new("HandleN8nCallbackCommand", AllowlistClassification.SystemCommand,
            "External N8n webhook callback handler", "Keep as-is"),

        // === Bootstrap/queries with no workspace/account scope ===
        ["GetBootstrapQuery"] = new("GetBootstrapQuery", AllowlistClassification.PublicCommand,
            "Public bootstrap data query", "Add IAnonymousRequest"),
        ["GetCurrentUserQuery"] = new("GetCurrentUserQuery", AllowlistClassification.PublicCommand,
            "Public current user query", "Add IAuthenticatedRequest"),

        // === Identity-based (no resource scope) ===
        ["UpdateProfileCommand"] = new("UpdateProfileCommand", AllowlistClassification.Intentional,
            "User updates own profile — identity-based, not resource-based", "Keep as-is"),

        // === Pre-hardening legacy gaps (no WorkspaceId property directly) ===
        // Collaboration — Comments (use ResourceId)
        ["GetCommentsQuery"] = new("GetCommentsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ResolveCommentCommand"] = new("ResolveCommentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateCommentCommand"] = new("CreateCommentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateCommentCommand"] = new("UpdateCommentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DeleteCommentCommand"] = new("DeleteCommentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // Collaboration — Activity (use ResourceId)
        ["GetResourceActivityQuery"] = new("GetResourceActivityQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // Collaboration — Attachments (use BoardItemId/ResourceId)
        ["GetBoardItemAttachmentsQuery"] = new("GetBoardItemAttachmentsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateBoardItemAttachmentCommand"] = new("CreateBoardItemAttachmentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DeleteAttachmentCommand"] = new("DeleteAttachmentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // WorkManagement — BoardGroups (use BoardId)
        ["UnarchiveBoardGroupCommand"] = new("UnarchiveBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DuplicateBoardGroupCommand"] = new("DuplicateBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ReorderBoardGroupsCommand"] = new("ReorderBoardGroupsCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateBoardGroupCommand"] = new("UpdateBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateBoardGroupCommand"] = new("CreateBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ArchiveBoardGroupCommand"] = new("ArchiveBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // WorkManagement — Checklists (use BoardItemId)
        ["GetChecklistsQuery"] = new("GetChecklistsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateChecklistItemCommand"] = new("UpdateChecklistItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateChecklistItemCommand"] = new("CreateChecklistItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateChecklistCommand"] = new("UpdateChecklistCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ToggleChecklistItemCommand"] = new("ToggleChecklistItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DeleteChecklistCommand"] = new("DeleteChecklistCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DeleteChecklistItemCommand"] = new("DeleteChecklistItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateChecklistCommand"] = new("CreateChecklistCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // WorkManagement — BoardViews (use BoardId)
        ["DeleteBoardViewCommand"] = new("DeleteBoardViewCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // WorkManagement — Labels (use BoardId)
        ["GetLabelsQuery"] = new("GetLabelsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateLabelCommand"] = new("CreateLabelCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateLabelCommand"] = new("UpdateLabelCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DeleteLabelCommand"] = new("DeleteLabelCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["RemoveLabelFromBoardItemCommand"] = new("RemoveLabelFromBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["AddLabelToBoardItemCommand"] = new("AddLabelToBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // WorkManagement — BoardItemLinks (use BoardItemId)
        ["DeleteBoardItemLinkCommand"] = new("DeleteBoardItemLinkCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateBoardItemLinkCommand"] = new("CreateBoardItemLinkCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // WorkManagement — BoardItems (use BoardItemId or BoardId)
        ["GetBoardItemQuery"] = new("GetBoardItemQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["GetMyBoardItemsQuery"] = new("GetMyBoardItemsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DuplicateBoardItemCommand"] = new("DuplicateBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateBoardItemCommand"] = new("UpdateBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UnlinkPageFromBoardItemCommand"] = new("UnlinkPageFromBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ArchiveBoardItemCommand"] = new("ArchiveBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["LinkPageToBoardItemCommand"] = new("LinkPageToBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateBoardItemStatusCommand"] = new("UpdateBoardItemStatusCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["SetBoardItemDueDateCommand"] = new("SetBoardItemDueDateCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UnassignBoardItemMemberCommand"] = new("UnassignBoardItemMemberCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateBoardItemFieldValuesCommand"] = new("UpdateBoardItemFieldValuesCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // WorkManagement — Board (use BoardId)
        ["GetFullBoardQuery"] = new("GetFullBoardQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateBoardBySlugCommand"] = new("CreateBoardBySlugCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UnarchiveBoardCommand"] = new("UnarchiveBoardCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["AddBoardMemberCommand"] = new("AddBoardMemberCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["RemoveBoardMemberCommand"] = new("RemoveBoardMemberCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DeleteBoardFieldCommand"] = new("DeleteBoardFieldCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ReorderBoardFieldsCommand"] = new("ReorderBoardFieldsCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateBoardFieldCommand"] = new("UpdateBoardFieldCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateBoardFieldCommand"] = new("CreateBoardFieldCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // Workspaces — query (no direct WorkspaceId in params)
        ["GetUserWorkspacesQuery"] = new("GetUserWorkspacesQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // Workspaces — token-scoped invitation
        ["AcceptInvitationCommand"] = new("AcceptInvitationCommand", AllowlistClassification.PublicCommand,
            "Token-scoped invitation command — auth required, no resource scope", "Keep as-is"),

        // Integrations — Calendar (use WorkspaceId from route/context)
        ["DisconnectCalendarCommand"] = new("DisconnectCalendarCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ConnectCalendarCommand"] = new("ConnectCalendarCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["TriggerCalendarSyncCommand"] = new("TriggerCalendarSyncCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // Governance — ShareLinks/Permissions
        ["DisableShareLinkCommand"] = new("DisableShareLinkCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["GetResourcePermissionsQuery"] = new("GetResourcePermissionsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["GrantResourcePermissionCommand"] = new("GrantResourcePermissionCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["RevokeResourcePermissionCommand"] = new("RevokeResourcePermissionCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // Automation
        ["GetAutomationExecutionsQuery"] = new("GetAutomationExecutionsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["GetWorkspaceAutomationsQuery"] = new("GetWorkspaceAutomationsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["SetAutomationRuleEnabledCommand"] = new("SetAutomationRuleEnabledCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateAutomationRuleCommand"] = new("CreateAutomationRuleCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // Documents — Blocks (use PageId)
        ["GetPageBlocksQuery"] = new("GetPageBlocksQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DeleteBlockCommand"] = new("DeleteBlockCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreateBlockCommand"] = new("CreateBlockCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ReorderBlocksCommand"] = new("ReorderBlocksCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["BatchUpdateBlocksCommand"] = new("BatchUpdateBlocksCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdateBlockCommand"] = new("UpdateBlockCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),

        // Documents — Pages (use PageId)
        ["GetWorkspacePagesQuery"] = new("GetWorkspacePagesQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["GetPageHistoryQuery"] = new("GetPageHistoryQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["SearchPagesQuery"] = new("SearchPagesQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["GetPageBreadcrumbQuery"] = new("GetPageBreadcrumbQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["GetPageQuery"] = new("GetPageQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["GetPageTreeQuery"] = new("GetPageTreeQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["MovePageCommand"] = new("MovePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["SetPageDeadlineCommand"] = new("SetPageDeadlineCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["CreatePageCommand"] = new("CreatePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["UpdatePageCommand"] = new("UpdatePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["PublishPageCommand"] = new("PublishPageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
        ["DeletePageCommand"] = new("DeletePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget),
    };

    [Fact]
    public void AllRequests_ShouldImplementSecurityClassification_OrBeAllowlisted()
    {
        var files = GetRequestFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (string.IsNullOrEmpty(name)) continue;
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            var isCommandOrQuery = declaration.Contains(": ICommand") || declaration.Contains(": IQuery");
            if (!isCommandOrQuery) continue;

            // Has explicit security interface
            if (SecurityInterfaces.Any(si => declaration.Contains(si))) continue;

            // Has IRequirePermission (implies authenticated + resource-scoped)
            if (declaration.Contains("IRequirePermission")) continue;

            // Known allowlist entry
            if (KnownUnclassified.ContainsKey(name)) continue;

            violations.Add($"{name}: {Path.GetFileName(file)}");
        }

        violations.Should().BeEmpty(
            $"All commands/queries must implement a security classification interface, implement IRequirePermission, " +
            $"have WorkspaceId property, or be in KnownUnclassified allowlist. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Allowlists_ShouldHaveNoDuplicateEntries()
    {
        var duplicates = KnownUnclassified.Keys
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            $"Allowlists must not contain duplicate entries. Duplicates: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Allowlists_ShouldHaveNonEmptyReasons()
    {
        var violations = new List<string>();

        foreach (var (name, entry) in KnownUnclassified)
        {
            if (string.IsNullOrWhiteSpace(entry.Reason))
                violations.Add($"'{name}' has empty reason");
            if (string.IsNullOrWhiteSpace(entry.TargetState))
                violations.Add($"'{name}' has empty target state");
        }

        violations.Should().BeEmpty(
            $"All allowlist entries must have non-empty reason and target state. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void LegacyGapEntries_ShouldHaveTargetState()
    {
        var violations = new List<string>();

        foreach (var (name, entry) in KnownUnclassified)
        {
            if (entry.Classification == AllowlistClassification.LegacyGap &&
                string.IsNullOrWhiteSpace(entry.TargetState))
            {
                violations.Add($"'{name}' is LegacyGap but has no target state");
            }
        }

        violations.Should().BeEmpty(
            $"LegacyGap entries must have a target state for burn-down. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void WorkspaceRequests_ShouldNotBeAnonymous()
    {
        var files = GetRequestFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (string.IsNullOrEmpty(name)) continue;

            if (declaration.Contains("IAnonymousRequest") && declaration.Contains("IWorkspaceRequest"))
                violations.Add($"{name}: {Path.GetFileName(file)}");
        }

        violations.Should().BeEmpty(
            $"Workspace-scoped requests must not also be anonymous. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void UnclassifiedCommands_Count_ShouldNotIncrease()
    {
        // Track the number of unclassified legacy-gap commands to prevent regression.
        // As commands are migrated to IWorkspaceRequest/IAccountRequest, remove them
        // from KnownUnclassified and decrement this count.
        var allowlistedCount = KnownUnclassified.Values.Count(e =>
            e.Classification == AllowlistClassification.LegacyGap ||
            e.Classification == AllowlistClassification.PublicCommand);

        var maxAllowed = 105;

        allowlistedCount.Should().BeLessThanOrEqualTo(maxAllowed,
            $"Unclassified commands count ({allowlistedCount}) exceeds maximum allowed ({maxAllowed}). " +
            $"Migrate commands to IWorkspaceRequest/IAccountRequest and remove from KnownUnclassified.");
    }
}
