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
        ["HandleCalendarWebhookCommand"] = new("HandleCalendarWebhookCommand", AllowlistClassification.Intentional,
            "Provider webhook ingestion declared INoDataRequest; durable effects flow through the integration pipeline",
            "Keep as-is; not a direct aggregate write"),
        ["ForgotPasswordCommand"] = new("ForgotPasswordCommand", AllowlistClassification.PublicCommand,
            "ForgotPassword is a global unauthenticated endpoint — no workspace context exists yet",
            "Keep as-is; public command does not need transactional behavior"),
        ["DisconnectCalendarCommand"] = new("DisconnectCalendarCommand", AllowlistClassification.Intentional,
            "Pre-hardening command missing IWriteRequest",
            "Add IWriteRequest"),
        ["ConnectCalendarCommand"] = new("ConnectCalendarCommand", AllowlistClassification.Intentional,
            "Pre-hardening command missing IWriteRequest",
            "Add IWriteRequest"),
        ["TriggerCalendarSyncCommand"] = new("TriggerCalendarSyncCommand", AllowlistClassification.Intentional,
            "Pre-hardening command missing IWriteRequest",
            "Add IWriteRequest"),
        ["MovePageCommand"] = new("MovePageCommand", AllowlistClassification.Intentional,
            "Pre-hardening command missing IWriteRequest",
            "Add IWriteRequest"),
        ["SetPageDeadlineCommand"] = new("SetPageDeadlineCommand", AllowlistClassification.Intentional,
            "Pre-hardening command missing IWriteRequest",
            "Add IWriteRequest"),
        ["ArchivePageCommand"] = new("ArchivePageCommand", AllowlistClassification.Intentional,
            "Pre-hardening command missing IWriteRequest",
            "Add IWriteRequest"),
        ["PublishPageCommand"] = new("PublishPageCommand", AllowlistClassification.Intentional,
            "Pre-hardening command missing IWriteRequest",
            "Add IWriteRequest"),
        ["StartOAuthLoginCommand"] = new("StartOAuthLoginCommand", AllowlistClassification.PublicCommand,
            "Non-mutating command: generates crypto + stores OAuth state in Redis, no DB mutation",
            "Keep as-is; read-only command does not need transactional behavior"),
        ["StartOAuthLinkCommand"] = new("StartOAuthLinkCommand", AllowlistClassification.Intentional,
            "Non-mutating authenticated command: generates crypto + stores OAuth link state in Redis, no DB mutation",
            "Keep as-is; read-only command does not need transactional behavior"),
    };

    private static readonly Dictionary<string, AllowlistEntry> KnownMissingWorkspaceRequest = new()
    {
        ["UpdateWorkspaceCommand"] = new("UpdateWorkspaceCommand", AllowlistClassification.Intentional,
            "Workspace command uses workspace ID from route, not IWorkspaceRequest marker",
            "Add IWorkspaceRequest for consistency with pipeline"),

        ["ConnectCalendarCommand"] = new("ConnectCalendarCommand", AllowlistClassification.Intentional,
            "Integration command missing workspace marker",
            "Add IWorkspaceRequest"),
        ["DisableShareLinkCommand"] = new("DisableShareLinkCommand", AllowlistClassification.Intentional,
            "Governance command missing workspace marker",
            "Add IWorkspaceRequest"),
        ["GrantResourcePermissionCommand"] = new("GrantResourcePermissionCommand", AllowlistClassification.Intentional,
            "Governance command missing workspace marker",
            "Add IWorkspaceRequest"),
        ["RevokeResourcePermissionCommand"] = new("RevokeResourcePermissionCommand", AllowlistClassification.Intentional,
            "Governance command missing workspace marker",
            "Add IWorkspaceRequest"),

        ["ProvisionPersonalWorkspaceCommand"] = new("ProvisionPersonalWorkspaceCommand", AllowlistClassification.SystemCommand,
            "System command triggered by user registration — no workspace context exists yet (WorkspaceId => null)",
            "Keep as-is; system command with null workspace scope"),
        ["SendWelcomeEmailCommand"] = new("SendWelcomeEmailCommand", AllowlistClassification.SystemCommand,
            "System command triggered by user registration — no workspace context exists yet (WorkspaceId => null)",
            "Keep as-is; system command with null workspace scope"),
        ["DeleteBoardViewCommand"] = new("DeleteBoardViewCommand", AllowlistClassification.Intentional,
            "WorkManagement command missing workspace marker", "Add IWorkspaceRequest"),
    };

    private static readonly Dictionary<string, AllowlistEntry> KnownMissingRequirePermission = new()
    {
        ["UpdateProfileCommand"] = new("UpdateProfileCommand", AllowlistClassification.Intentional,
            "User updates own profile — permission is identity-based, not resource-based", "Keep as-is"),
        ["UpdateEmailCommand"] = new("UpdateEmailCommand", AllowlistClassification.Intentional,
            "User updates own email — permission is identity-based, not resource-based", "Keep as-is"),
        ["CreateBoardBySlugCommand"] = new("CreateBoardBySlugCommand", AllowlistClassification.Intentional,
            "Module authorized via workspace-role policy (AccessControlBehavior); no fine-grained PermissionAction exists.", "Keep as-is; intentional non-fine-grained authorization"),
        ["UpdateWorkspaceCommand"] = new("UpdateWorkspaceCommand", AllowlistClassification.Intentional,
            "Module authorized via workspace-role policy (AccessControlBehavior); no fine-grained PermissionAction exists.", "Keep as-is; intentional non-fine-grained authorization"),
        ["ArchiveWorkspaceBySlugCommand"] = new("ArchiveWorkspaceBySlugCommand", AllowlistClassification.Intentional,
            "Module authorized via workspace-role policy (AccessControlBehavior); no fine-grained PermissionAction exists.", "Keep as-is; intentional non-fine-grained authorization"),
        ["RemoveMemberBySlugCommand"] = new("RemoveMemberBySlugCommand", AllowlistClassification.Intentional,
            "Module authorized via workspace-role policy (AccessControlBehavior); no fine-grained PermissionAction exists.", "Keep as-is; intentional non-fine-grained authorization"),
        ["InviteMemberBySlugCommand"] = new("InviteMemberBySlugCommand", AllowlistClassification.Intentional,
            "Module authorized via workspace-role policy (AccessControlBehavior); no fine-grained PermissionAction exists.", "Keep as-is; intentional non-fine-grained authorization"),


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
    public void MutatingCommands_ShouldImplement_IWriteRequest()
    {
        var staleViolations = new List<string>();
        var files = GetCommandFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (name.EndsWith("Dto") || name.EndsWith("Response")) continue;

            if (declaration.Contains(": ICommand"))
            {
                if (!declaration.Contains("IWriteRequest"))
                {
                    if (!KnownMissingTransactionalRequest.ContainsKey(name))
                        violations.Add(
                            $"rule=IWriteRequest; request={name}; category=UNCLASSIFIED; " +
                            $"current={Path.GetFileName(file)} lacks IWriteRequest; action=add marker or allowlist entry");
                }
                else if (KnownMissingTransactionalRequest.TryGetValue(name, out var staleEntry))
                {
                    staleViolations.Add(
                        $"rule=IWriteRequest; request={name}; category={staleEntry.Classification}; " +
                        $"reason='{staleEntry.Reason}'; current=marker now present; action=DELETE stale allowlist entry");
                }
            }
        }

        violations.Should().BeEmpty(
            $"New mutating commands (ICommand) must implement IWriteRequest. " +
            $"Violations: {string.Join(", ", violations)}");
        staleViolations.Should().BeEmpty(
            "STALE ALLOWLIST ENTRIES DETECTED — the underlying condition no longer holds. " +
            string.Join(" | ", staleViolations));
    }

    [Fact]
    public void MutatingCommands_WithWorkspaceId_ShouldImplement_IWorkspaceRequest()
    {
        var staleViolations = new List<string>();
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
                    violations.Add(
                        $"rule=IWorkspaceRequest; request={name}; category=UNCLASSIFIED; " +
                        $"current={Path.GetFileName(file)} has WorkspaceId but no IWorkspaceRequest; " +
                        "action=add marker or allowlist entry");
            }
            else if (KnownMissingWorkspaceRequest.TryGetValue(name, out var staleWs))
            {
                staleViolations.Add(
                    $"rule=IWorkspaceRequest; request={name}; category={staleWs.Classification}; " +
                    $"reason='{staleWs.Reason}'; current=marker now present; action=DELETE stale allowlist entry");
            }
        }

        violations.Should().BeEmpty(
            $"New commands with WorkspaceId must implement IWorkspaceRequest. " +
            $"Violations: {string.Join(", ", violations)}");
        staleViolations.Should().BeEmpty(
            "STALE ALLOWLIST ENTRIES DETECTED — the underlying condition no longer holds. " +
            string.Join(" | ", staleViolations));
    }

    [Fact]
    public void CreateUpdateDeleteCommands_ShouldImplement_IRequirePermission()
    {
        var staleViolations = new List<string>();
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
                    violations.Add(
                        $"rule=IRequirePermission; request={name}; category=UNCLASSIFIED; " +
                        $"current={Path.GetFileName(file)} lacks IRequirePermission; action=add marker or allowlist entry");
            }
            else if (KnownMissingRequirePermission.TryGetValue(name, out var stalePerm))
            {
                staleViolations.Add(
                    $"rule=IRequirePermission; request={name}; category={stalePerm.Classification}; " +
                    $"reason='{stalePerm.Reason}'; current=marker now present; action=DELETE stale allowlist entry");
            }
        }

        violations.Should().BeEmpty(
            $"New Create/Update/Delete/Archive/Restore/Invite/Remove/Assign commands must implement IRequirePermission. " +
            $"Violations: {string.Join(", ", violations)}");
        staleViolations.Should().BeEmpty(
            "STALE ALLOWLIST ENTRIES DETECTED — the underlying condition no longer holds. " +
            string.Join(" | ", staleViolations));
    }

    [Fact]
    public void CommandsImplementingIWriteRequest_WithWorkspaceId_ShouldAlsoImplement_IWorkspaceRequest()
    {
        var files = GetCommandFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = RemoveComments(File.ReadAllText(file));
            var declaration = ReadDeclaration(content);
            if (string.IsNullOrEmpty(declaration)) continue;

            var name = ExtractRecordName(declaration);
            if (!declaration.Contains("IWriteRequest")) continue;
            if (declaration.Contains("IWorkspaceRequest")) continue;

            var hasWorkspaceId = content.Contains("Guid WorkspaceId") || content.Contains("Guid? WorkspaceId");
            if (hasWorkspaceId)
            {
                if (!KnownMissingWorkspaceRequest.ContainsKey(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty(
            $"Commands with IWriteRequest and WorkspaceId must also implement IWorkspaceRequest. " +
            $"Fix by adding to KnownMissingWorkspaceRequest with classification, or add IWorkspaceRequest. " +
            $"Violations: {string.Join(", ", violations)}");
    }
}
