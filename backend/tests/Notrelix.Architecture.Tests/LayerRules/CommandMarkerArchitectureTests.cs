namespace Notrelix.Architecture.Tests;

public class CommandMarkerArchitectureTests
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

    private static string[] GetCommandFiles()
    {
        var appPath = GetApplicationPath();
        return Directory.GetFiles(Path.Combine(appPath, "Features"), "*.cs", SearchOption.AllDirectories)
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

    // --- Classified allowlists ---

    private static readonly Dictionary<string, AllowlistEntry> KnownMissingTransactionalRequest = new()
    {
        ["ForgotPasswordCommand"] = new("ForgotPasswordCommand", AllowlistClassification.PublicCommand,
            "ForgotPassword is a global unauthenticated endpoint — no workspace context exists yet",
            "Keep as-is; public command does not need transactional behavior"),
        ["ToggleChecklistItemCommand"] = new("ToggleChecklistItemCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["DeleteBoardItemLinkCommand"] = new("DeleteBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["CreateBoardItemLinkCommand"] = new("CreateBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["HandleCalendarWebhookCommand"] = new("HandleCalendarWebhookCommand", AllowlistClassification.SystemCommand,
            "External webhook handler — transaction managed by integration adapter",
            "Keep as-is; system command with explicit transaction management"),
        ["DisconnectCalendarCommand"] = new("DisconnectCalendarCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["ConnectCalendarCommand"] = new("ConnectCalendarCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["TriggerCalendarSyncCommand"] = new("TriggerCalendarSyncCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["MovePageCommand"] = new("MovePageCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["SetPageDeadlineCommand"] = new("SetPageDeadlineCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["PublishPageCommand"] = new("PublishPageCommand", AllowlistClassification.LegacyGap,
            "Pre-hardening command missing ITransactionalRequest",
            "Add ITransactionalRequest"),
        ["StartOAuthLoginCommand"] = new("StartOAuthLoginCommand", AllowlistClassification.PublicCommand,
            "Non-mutating command: generates crypto + stores OAuth state in Redis, no DB mutation",
            "Keep as-is; read-only command does not need transactional behavior"),
    };

    private static readonly Dictionary<string, AllowlistEntry> KnownMissingWorkspaceRequest = new()
    {
        ["UpdateWorkspaceCommand"] = new("UpdateWorkspaceCommand", AllowlistClassification.LegacyGap,
            "Workspace command uses workspace ID from route, not IWorkspaceRequest marker",
            "Add IWorkspaceRequest for consistency with pipeline"),
        ["ArchiveWorkspaceCommand"] = new("ArchiveWorkspaceCommand", AllowlistClassification.LegacyGap,
            "Workspace command uses workspace ID from route",
            "Add IWorkspaceRequest"),
        ["RemoveMemberCommand"] = new("RemoveMemberCommand", AllowlistClassification.LegacyGap,
            "Workspace member command uses workspace ID from route",
            "Add IWorkspaceRequest"),
        ["UpdateMemberRoleCommand"] = new("UpdateMemberRoleCommand", AllowlistClassification.LegacyGap,
            "Workspace member command uses workspace ID from route",
            "Add IWorkspaceRequest"),
        ["CancelInvitationCommand"] = new("CancelInvitationCommand", AllowlistClassification.LegacyGap,
            "Workspace invitation command uses workspace ID from route",
            "Add IWorkspaceRequest"),

        ["ConnectCalendarCommand"] = new("ConnectCalendarCommand", AllowlistClassification.LegacyGap,
            "Integration command missing workspace marker",
            "Add IWorkspaceRequest"),
        ["DisableShareLinkCommand"] = new("DisableShareLinkCommand", AllowlistClassification.LegacyGap,
            "Governance command missing workspace marker",
            "Add IWorkspaceRequest"),
        ["GrantResourcePermissionCommand"] = new("GrantResourcePermissionCommand", AllowlistClassification.LegacyGap,
            "Governance command missing workspace marker",
            "Add IWorkspaceRequest"),
        ["RevokeResourcePermissionCommand"] = new("RevokeResourcePermissionCommand", AllowlistClassification.LegacyGap,
            "Governance command missing workspace marker",
            "Add IWorkspaceRequest"),
        ["CreateAutomationRuleCommand"] = new("CreateAutomationRuleCommand", AllowlistClassification.LegacyGap,
            "Automation command missing workspace marker",
            "Add IWorkspaceRequest"),
        ["CreatePageCommand"] = new("CreatePageCommand", AllowlistClassification.LegacyGap,
            "Document command missing workspace marker",
            "Add IWorkspaceRequest"),

        ["ProvisionPersonalWorkspaceCommand"] = new("ProvisionPersonalWorkspaceCommand", AllowlistClassification.SystemCommand,
            "System command triggered by user registration — no workspace context exists yet (WorkspaceId => null)",
            "Keep as-is; system command with null workspace scope"),
        ["SendWelcomeEmailCommand"] = new("SendWelcomeEmailCommand", AllowlistClassification.SystemCommand,
            "System command triggered by user registration — no workspace context exists yet (WorkspaceId => null)",
            "Keep as-is; system command with null workspace scope"),
        ["DeleteBoardViewCommand"] = new("DeleteBoardViewCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
    };

    private static readonly Dictionary<string, AllowlistEntry> KnownMissingRequirePermission = new()
    {
        ["CreateCommentCommand"] = new("CreateCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["UpdateCommentCommand"] = new("UpdateCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["DeleteCommentCommand"] = new("DeleteCommentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["CreateBoardItemAttachmentCommand"] = new("CreateBoardItemAttachmentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["UpdateProfileCommand"] = new("UpdateProfileCommand", AllowlistClassification.Intentional,
            "User updates own profile — permission is identity-based, not resource-based", "Keep as-is"),
        ["UpdateBoardGroupCommand"] = new("UpdateBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateBoardGroupCommand"] = new("CreateBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ArchiveBoardGroupCommand"] = new("ArchiveBoardGroupCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateChecklistItemCommand"] = new("UpdateChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateChecklistItemCommand"] = new("CreateChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateChecklistCommand"] = new("UpdateChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteChecklistCommand"] = new("DeleteChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteChecklistItemCommand"] = new("DeleteChecklistItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateChecklistCommand"] = new("CreateChecklistCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateLabelCommand"] = new("CreateLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateLabelCommand"] = new("UpdateLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteLabelCommand"] = new("DeleteLabelCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["RemoveLabelFromBoardItemCommand"] = new("RemoveLabelFromBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteBoardItemLinkCommand"] = new("DeleteBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateBoardItemLinkCommand"] = new("CreateBoardItemLinkCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateCardCommand"] = new("CreateCardCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardItemCommand"] = new("UpdateBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ArchiveBoardItemCommand"] = new("ArchiveBoardItemCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardItemStatusCommand"] = new("UpdateBoardItemStatusCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardItemFieldValuesCommand"] = new("UpdateBoardItemFieldValuesCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateBoardBySlugCommand"] = new("CreateBoardBySlugCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["ArchiveBoardCommand"] = new("ArchiveBoardCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["RemoveBoardMemberCommand"] = new("RemoveBoardMemberCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["DeleteBoardFieldCommand"] = new("DeleteBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateBoardFieldCommand"] = new("UpdateBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["CreateBoardFieldCommand"] = new("CreateBoardFieldCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
        ["UpdateWorkspaceCommand"] = new("UpdateWorkspaceCommand", AllowlistClassification.LegacyGap,
            "Workspace command missing permission marker", "Add IRequirePermission"),
        ["CreateWorkspaceCommand"] = new("CreateWorkspaceCommand", AllowlistClassification.Intentional,
            "Global command — user creates their own workspace, no pre-existing permission context", "Keep as-is"),
        ["ArchiveWorkspaceBySlugCommand"] = new("ArchiveWorkspaceBySlugCommand", AllowlistClassification.LegacyGap,
            "Workspace command missing permission marker", "Add IRequirePermission"),
        ["ArchiveWorkspaceCommand"] = new("ArchiveWorkspaceCommand", AllowlistClassification.LegacyGap,
            "Workspace command missing permission marker", "Add IRequirePermission"),
        ["RemoveMemberCommand"] = new("RemoveMemberCommand", AllowlistClassification.LegacyGap,
            "Workspace member command missing permission marker", "Add IRequirePermission"),
        ["RemoveMemberBySlugCommand"] = new("RemoveMemberBySlugCommand", AllowlistClassification.LegacyGap,
            "Workspace member command missing permission marker", "Add IRequirePermission"),
        ["UpdateMemberRoleCommand"] = new("UpdateMemberRoleCommand", AllowlistClassification.LegacyGap,
            "Workspace member command missing permission marker", "Add IRequirePermission"),
        ["UpdateMemberRoleBySlugCommand"] = new("UpdateMemberRoleBySlugCommand", AllowlistClassification.LegacyGap,
            "Workspace member command missing permission marker", "Add IRequirePermission"),
        ["InviteMemberBySlugCommand"] = new("InviteMemberBySlugCommand", AllowlistClassification.LegacyGap,
            "Workspace invitation command missing permission marker", "Add IRequirePermission"),

        ["CreateAutomationRuleCommand"] = new("CreateAutomationRuleCommand", AllowlistClassification.LegacyGap,
            "Automation command missing permission marker", "Add IRequirePermission"),
        ["DeleteBlockCommand"] = new("DeleteBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["CreateBlockCommand"] = new("CreateBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["UpdateBlockCommand"] = new("UpdateBlockCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["CreatePageCommand"] = new("CreatePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["UpdatePageCommand"] = new("UpdatePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),
        ["DeletePageCommand"] = new("DeletePageCommand", AllowlistClassification.LegacyGap,
            "Documents command missing permission marker", "Add IRequirePermission"),

        ["DeleteAttachmentCommand"] = new("DeleteAttachmentCommand", AllowlistClassification.LegacyGap,
            "Collaboration command missing permission marker", "Add IRequirePermission"),
        ["DeleteBoardViewCommand"] = new("DeleteBoardViewCommand", AllowlistClassification.LegacyGap,
            "WorkManagement command missing permission marker", "Add IRequirePermission"),
    };

    // --- Validation tests ---

    [Fact]
    public void Allowlists_ShouldHaveNoDuplicateEntries()
    {
        var allAllowlists = new Dictionary<string, Dictionary<string, AllowlistEntry>>
        {
            ["TransactionalRequest"] = KnownMissingTransactionalRequest,
            ["WorkspaceRequest"] = KnownMissingWorkspaceRequest,
            ["RequirePermission"] = KnownMissingRequirePermission,
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

    [Fact]
    public void Allowlists_ShouldHaveNonEmptyReasons()
    {
        var allAllowlists = new Dictionary<string, Dictionary<string, AllowlistEntry>>
        {
            ["TransactionalRequest"] = KnownMissingTransactionalRequest,
            ["WorkspaceRequest"] = KnownMissingWorkspaceRequest,
            ["RequirePermission"] = KnownMissingRequirePermission,
        };

        var violations = new List<string>();

        foreach (var (listName, allowlist) in allAllowlists)
        {
            foreach (var (name, entry) in allowlist)
            {
                if (string.IsNullOrWhiteSpace(entry.Reason))
                    violations.Add($"{listName}: '{name}' has empty reason");
                if (string.IsNullOrWhiteSpace(entry.TargetState))
                    violations.Add($"{listName}: '{name}' has empty target state");
            }
        }

        violations.Should().BeEmpty(
            $"All allowlist entries must have non-empty reason and target state. Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void LegacyGapEntries_ShouldHaveTargetState()
    {
        var allAllowlists = new Dictionary<string, Dictionary<string, AllowlistEntry>>
        {
            ["TransactionalRequest"] = KnownMissingTransactionalRequest,
            ["WorkspaceRequest"] = KnownMissingWorkspaceRequest,
            ["RequirePermission"] = KnownMissingRequirePermission,
        };

        var violations = new List<string>();

        foreach (var (listName, allowlist) in allAllowlists)
        {
            foreach (var (name, entry) in allowlist)
            {
                if (entry.Classification == AllowlistClassification.LegacyGap &&
                    string.IsNullOrWhiteSpace(entry.TargetState))
                {
                    violations.Add($"{listName}: '{name}' is LegacyGap but has no target state");
                }
            }
        }

        violations.Should().BeEmpty(
            $"LegacyGap entries must have a target state for burn-down. Violations: {string.Join(", ", violations)}");
    }

    // --- Marker enforcement tests ---

    [Fact]
    public void MutatingCommands_ShouldImplement_ITransactionalRequest()
    {
        var files = GetCommandFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (declaration.Contains(": ICommand") && !declaration.Contains("ITransactionalRequest"))
            {
                if (!KnownMissingTransactionalRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"New mutating commands (ICommand) must implement ITransactionalRequest. " +
            $"Fix by adding to KnownMissingTransactionalRequest with classification, or add ITransactionalRequest. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void MutatingCommands_WithWorkspaceId_ShouldImplement_IWorkspaceRequest()
    {
        var files = GetCommandFiles();
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
                if (!KnownMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"New commands with WorkspaceId must implement IWorkspaceRequest. " +
            $"Fix by adding to KnownMissingWorkspaceRequest with classification, or add IWorkspaceRequest. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CreateUpdateDeleteCommands_ShouldImplement_IRequirePermission()
    {
        var files = GetCommandFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            var isCrud = name.StartsWith("Create") || name.StartsWith("Update")
                      || name.StartsWith("Delete") || name.StartsWith("Archive")
                      || name.StartsWith("Restore") || name.StartsWith("Remove")
                      || name.StartsWith("Invite") || name.StartsWith("Assign");

            if (!isCrud) continue;

            if (!declaration.Contains("IRequirePermission"))
            {
                if (!KnownMissingRequirePermission.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"New Create/Update/Delete/Archive/Restore/Invite/Remove/Assign commands must implement IRequirePermission. " +
            $"Fix by adding to KnownMissingRequirePermission with classification, or add IRequirePermission. " +
            $"Violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void CommandsImplementingITransactionalRequest_WithWorkspaceId_ShouldAlsoImplement_IWorkspaceRequest()
    {
        var files = GetCommandFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (!declaration.Contains("ITransactionalRequest")) continue;
            if (declaration.Contains("IWorkspaceRequest")) continue;

            var hasWorkspaceId = content.Contains("Guid WorkspaceId") || content.Contains("Guid? WorkspaceId");
            if (hasWorkspaceId)
            {
                if (!KnownMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Commands with ITransactionalRequest and WorkspaceId must also implement IWorkspaceRequest. " +
            $"Fix by adding to KnownMissingWorkspaceRequest with classification, or add IWorkspaceRequest. " +
            $"Violations: {string.Join(", ", violations)}");
    }
}
