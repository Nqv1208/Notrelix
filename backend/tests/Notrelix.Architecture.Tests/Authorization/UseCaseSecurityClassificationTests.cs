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

    private static bool HasWorkspaceIdInDeclaration(string declaration)
    {
        return Regex.IsMatch(
            declaration,
            @"\bGuid\??\s+WorkspaceId\b",
            RegexOptions.Compiled);
    }

    private static bool HasWorkspaceIdProperty(string content)
    {
        var sanitized = RemoveComments(content);
        return Regex.IsMatch(
            sanitized,
            @"public\s+Guid\??\s+WorkspaceId\s*\{\s*get;",
            RegexOptions.Compiled);
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
        "IAccountRequest",
        "ITokenScopedRequest"
    ];

    /// <summary>
    /// Commands and queries that are intentionally unclassified (public, system, identity-based)
    /// or are pre-hardening legacy gaps that don't carry WorkspaceId directly.
    /// </summary>
    private static readonly Dictionary<string, AllowlistEntry> KnownUnclassified = new()
    {
        // === Public auth commands ===
        ["LoginCommand"] = new("LoginCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest", Feature: "Identity"),
        ["LogoutCommand"] = new("LogoutCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest", Feature: "Identity"),
        ["RefreshTokenCommand"] = new("RefreshTokenCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest", Feature: "Identity"),
        ["ForgotPasswordCommand"] = new("ForgotPasswordCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest", Feature: "Identity"),
        ["ResetPasswordCommand"] = new("ResetPasswordCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest", Feature: "Identity"),
        ["RegisterCommand"] = new("RegisterCommand", AllowlistClassification.PublicCommand,
            "Public unauthenticated command", "Add IAnonymousRequest", Feature: "Identity"),

        // === System commands (no workspace/user scope) ===
        ["SendWelcomeEmailCommand"] = new("SendWelcomeEmailCommand", AllowlistClassification.SystemCommand,
            "System command triggered by user registration", "Keep as-is", Feature: "Identity"),
        ["ProvisionPersonalWorkspaceCommand"] = new("ProvisionPersonalWorkspaceCommand", AllowlistClassification.SystemCommand,
            "System command triggered by user registration", "Keep as-is", Feature: "Identity"),
        ["HandleCalendarWebhookCommand"] = new("HandleCalendarWebhookCommand", AllowlistClassification.SystemCommand,
            "External webhook handler", "Keep as-is", Feature: "Integrations"),
        ["HandleN8nCallbackCommand"] = new("HandleN8nCallbackCommand", AllowlistClassification.SystemCommand,
            "External N8n webhook callback handler", "Keep as-is", Feature: "Automation"),

        // === Bootstrap/queries with no workspace/account scope ===
        ["GetBootstrapQuery"] = new("GetBootstrapQuery", AllowlistClassification.PublicCommand,
            "Public bootstrap data query", "Add IAnonymousRequest", Feature: "Accounts"),
        ["GetCurrentUserQuery"] = new("GetCurrentUserQuery", AllowlistClassification.PublicCommand,
            "Public current user query", "Add IAuthenticatedRequest", Feature: "Identity"),

        // === Identity-based (no resource scope) ===
        ["UpdateProfileCommand"] = new("UpdateProfileCommand", AllowlistClassification.Intentional,
            "User updates own profile — identity-based, not resource-based", "Keep as-is", Feature: "Identity"),

        // === Pre-hardening legacy gaps (no WorkspaceId property directly) ===
        // Collaboration — Comments (use ResourceId)
        ["GetCommentsQuery"] = new("GetCommentsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),
        ["ResolveCommentCommand"] = new("ResolveCommentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),
        ["CreateCommentCommand"] = new("CreateCommentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),
        ["UpdateCommentCommand"] = new("UpdateCommentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),
        ["DeleteCommentCommand"] = new("DeleteCommentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),

        // Collaboration — Activity (use ResourceId)
        ["GetResourceActivityQuery"] = new("GetResourceActivityQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),

        // Collaboration — Attachments (use BoardItemId/ResourceId)
        ["GetBoardItemAttachmentsQuery"] = new("GetBoardItemAttachmentsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),
        ["CreateBoardItemAttachmentCommand"] = new("CreateBoardItemAttachmentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),
        ["DeleteAttachmentCommand"] = new("DeleteAttachmentCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Collaboration"),

        // WorkManagement — BoardGroups (use BoardId)
        ["UnarchiveBoardGroupCommand"] = new("UnarchiveBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["DuplicateBoardGroupCommand"] = new("DuplicateBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["ReorderBoardGroupsCommand"] = new("ReorderBoardGroupsCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UpdateBoardGroupCommand"] = new("UpdateBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["CreateBoardGroupCommand"] = new("CreateBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["ArchiveBoardGroupCommand"] = new("ArchiveBoardGroupCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),

        // WorkManagement — Checklists (use BoardItemId)
        ["GetChecklistsQuery"] = new("GetChecklistsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UpdateChecklistItemCommand"] = new("UpdateChecklistItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["CreateChecklistItemCommand"] = new("CreateChecklistItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UpdateChecklistCommand"] = new("UpdateChecklistCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["ToggleChecklistItemCommand"] = new("ToggleChecklistItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["DeleteChecklistCommand"] = new("DeleteChecklistCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["DeleteChecklistItemCommand"] = new("DeleteChecklistItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["CreateChecklistCommand"] = new("CreateChecklistCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),

        // WorkManagement — BoardViews (use BoardId)
        ["DeleteBoardViewCommand"] = new("DeleteBoardViewCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),

        // WorkManagement — Labels (use BoardId)
        ["GetLabelsQuery"] = new("GetLabelsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["CreateLabelCommand"] = new("CreateLabelCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UpdateLabelCommand"] = new("UpdateLabelCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["DeleteLabelCommand"] = new("DeleteLabelCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["RemoveLabelFromBoardItemCommand"] = new("RemoveLabelFromBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["AddLabelToBoardItemCommand"] = new("AddLabelToBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),

        // WorkManagement — BoardItemLinks (use BoardItemId)
        ["DeleteBoardItemLinkCommand"] = new("DeleteBoardItemLinkCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["CreateBoardItemLinkCommand"] = new("CreateBoardItemLinkCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),

        // WorkManagement — BoardItems (use BoardItemId or BoardId)
        ["GetBoardItemQuery"] = new("GetBoardItemQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["GetMyBoardItemsQuery"] = new("GetMyBoardItemsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["DuplicateBoardItemCommand"] = new("DuplicateBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UpdateBoardItemCommand"] = new("UpdateBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UnlinkPageFromBoardItemCommand"] = new("UnlinkPageFromBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["ArchiveBoardItemCommand"] = new("ArchiveBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["LinkPageToBoardItemCommand"] = new("LinkPageToBoardItemCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UpdateBoardItemStatusCommand"] = new("UpdateBoardItemStatusCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["SetBoardItemDueDateCommand"] = new("SetBoardItemDueDateCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UnassignBoardItemMemberCommand"] = new("UnassignBoardItemMemberCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UpdateBoardItemFieldValuesCommand"] = new("UpdateBoardItemFieldValuesCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),

        // WorkManagement — Board (use BoardId)
        ["GetFullBoardQuery"] = new("GetFullBoardQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["CreateBoardBySlugCommand"] = new("CreateBoardBySlugCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UnarchiveBoardCommand"] = new("UnarchiveBoardCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["AddBoardMemberCommand"] = new("AddBoardMemberCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["RemoveBoardMemberCommand"] = new("RemoveBoardMemberCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["DeleteBoardFieldCommand"] = new("DeleteBoardFieldCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["ReorderBoardFieldsCommand"] = new("ReorderBoardFieldsCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["UpdateBoardFieldCommand"] = new("UpdateBoardFieldCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),
        ["CreateBoardFieldCommand"] = new("CreateBoardFieldCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "WorkManagement"),

        // Integrations — Calendar (use WorkspaceId from route/context)
        ["DisconnectCalendarCommand"] = new("DisconnectCalendarCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Integrations"),
        ["ConnectCalendarCommand"] = new("ConnectCalendarCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Integrations"),
        ["TriggerCalendarSyncCommand"] = new("TriggerCalendarSyncCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Integrations"),

        // Governance — ShareLinks/Permissions
        ["DisableShareLinkCommand"] = new("DisableShareLinkCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Governance"),
        ["GetResourcePermissionsQuery"] = new("GetResourcePermissionsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Governance"),
        ["GrantResourcePermissionCommand"] = new("GrantResourcePermissionCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Governance"),
        ["RevokeResourcePermissionCommand"] = new("RevokeResourcePermissionCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Governance"),

        // Automation
        ["GetAutomationExecutionsQuery"] = new("GetAutomationExecutionsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Automation"),
        ["GetWorkspaceAutomationsQuery"] = new("GetWorkspaceAutomationsQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Automation"),
        ["SetAutomationRuleEnabledCommand"] = new("SetAutomationRuleEnabledCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Automation"),
        ["CreateAutomationRuleCommand"] = new("CreateAutomationRuleCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Automation"),

        // Documents — Blocks (use PageId)
        ["GetPageBlocksQuery"] = new("GetPageBlocksQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["DeleteBlockCommand"] = new("DeleteBlockCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["CreateBlockCommand"] = new("CreateBlockCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["ReorderBlocksCommand"] = new("ReorderBlocksCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["BatchUpdateBlocksCommand"] = new("BatchUpdateBlocksCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["UpdateBlockCommand"] = new("UpdateBlockCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),

        // Documents — Pages (use PageId)
        ["GetWorkspacePagesQuery"] = new("GetWorkspacePagesQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["GetPageHistoryQuery"] = new("GetPageHistoryQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["SearchPagesQuery"] = new("SearchPagesQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["GetPageBreadcrumbQuery"] = new("GetPageBreadcrumbQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["GetPageQuery"] = new("GetPageQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["GetPageTreeQuery"] = new("GetPageTreeQuery", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["MovePageCommand"] = new("MovePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["SetPageDeadlineCommand"] = new("SetPageDeadlineCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["CreatePageCommand"] = new("CreatePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["UpdatePageCommand"] = new("UpdatePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["PublishPageCommand"] = new("PublishPageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
        ["DeletePageCommand"] = new("DeletePageCommand", AllowlistClassification.LegacyGap, LegacyGapReason, LegacyGapTarget, Feature: "Documents"),
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
            $"All commands/queries must implement a security classification interface " +
            $"(IAnonymousRequest, IAuthenticatedRequest, ISystemInternalRequest, IAccountRequest, " +
            $"IWorkspaceRequest, ITokenScopedRequest), " +
            $"implement IRequirePermission where authorization is required, " +
            $"or be explicitly documented in KnownUnclassified with a non-legacy reason. " +
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
        var allowlistedCount = KnownUnclassified.Values.Count(e =>
            e.Classification == AllowlistClassification.LegacyGap ||
            e.Classification == AllowlistClassification.PublicCommand);

        var maxAllowed = 90;

        allowlistedCount.Should().BeLessThanOrEqualTo(maxAllowed,
            $"Unclassified commands count ({allowlistedCount}) exceeds maximum allowed ({maxAllowed}). " +
            $"Migrate commands to IWorkspaceRequest/IAccountRequest and remove from KnownUnclassified.");
    }

    [Fact]
    public void WorkspaceFeature_ShouldNotContainLegacySecurityAllowlistEntries()
    {
        var legacyWorkspaceEntries = KnownUnclassified
            .Where(x => x.Value.Feature == "Workspaces")
            .Where(x => x.Value.Classification == AllowlistClassification.LegacyGap)
            .Select(x => x.Key)
            .ToList();

        legacyWorkspaceEntries.Should().BeEmpty(
            "Workspace foundation is hard-locked and must not contain legacy security gaps.");
    }

    [Fact]
    public void WorkspaceIdDetection_ShouldIgnoreComments()
    {
        var declarationWithParam =
            "public record FooCommand(Guid WorkspaceId) : ICommand<Unit>, IWorkspaceRequest";

        HasWorkspaceIdInDeclaration(declarationWithParam).Should().BeTrue();

        var contentWithCommentOnly = @"
            // Guid WorkspaceId
            /* public Guid WorkspaceId { get; } */
            public record FooCommand(string Name) : ICommand<Unit>;
        ";

        HasWorkspaceIdProperty(contentWithCommentOnly).Should().BeFalse(
            "WorkspaceId in comments should not be detected as a real property");
    }
}
