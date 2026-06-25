using System.Text.RegularExpressions;

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

    private static readonly HashSet<string> KnownMissingTransactionalRequest =
    [
        "ForgotPasswordCommand", "ToggleChecklistItemCommand",
        "DeleteBoardItemLinkCommand", "CreateBoardItemLinkCommand",
        "HandleCalendarWebhookCommand", "DisconnectCalendarCommand",
        "ConnectCalendarCommand", "TriggerCalendarSyncCommand",
        "MovePageCommand", "SetPageDeadlineCommand",
        "ArchivePageCommand", "PublishPageCommand",
    ];

    private static readonly HashSet<string> KnownMissingWorkspaceRequest =
    [
        "UpdateWorkspaceCommand", "ArchiveWorkspaceCommand",
        "RemoveMemberCommand", "UpdateMemberRoleCommand",
        "CancelInvitationCommand", "InviteMemberCommand",
        "ConnectCalendarCommand",
        "DisableShareLinkCommand",
        "GrantResourcePermissionCommand", "RevokeResourcePermissionCommand",
        "CreateAutomationRuleCommand", "CreatePageCommand",
    ];

    private static readonly HashSet<string> KnownMissingRequirePermission =
    [
        "CreateCommentCommand", "UpdateCommentCommand", "DeleteCommentCommand",
        "CreateBoardItemAttachmentCommand", "UpdateProfileCommand",
        "UpdateBoardGroupCommand", "CreateBoardGroupCommand", "ArchiveBoardGroupCommand",
        "UpdateChecklistItemCommand", "CreateChecklistItemCommand",
        "UpdateChecklistCommand", "DeleteChecklistCommand",
        "DeleteChecklistItemCommand", "CreateChecklistCommand",
        "CreateLabelCommand", "UpdateLabelCommand", "DeleteLabelCommand",
        "RemoveLabelFromBoardItemCommand",
        "DeleteBoardItemLinkCommand", "CreateBoardItemLinkCommand",
        "CreateCardCommand", "UpdateBoardItemCommand",
        "ArchiveBoardItemCommand", "UpdateBoardItemStatusCommand",
        "UpdateBoardItemFieldValuesCommand", "CreateBoardBySlugCommand",
        "ArchiveBoardCommand", "RemoveBoardMemberCommand",
        "DeleteBoardFieldCommand", "UpdateBoardFieldCommand", "CreateBoardFieldCommand",
        "UpdateWorkspaceCommand", "CreateWorkspaceCommand",
        "ArchiveWorkspaceBySlugCommand", "ArchiveWorkspaceCommand",
        "RemoveMemberCommand", "RemoveMemberBySlugCommand",
        "UpdateMemberRoleCommand", "UpdateMemberRoleBySlugCommand",
        "InviteMemberBySlugCommand", "InviteMemberCommand",
        "CreateAutomationRuleCommand",
        "DeleteBlockCommand", "CreateBlockCommand", "UpdateBlockCommand",
        "CreatePageCommand", "UpdatePageCommand", "ArchivePageCommand", "DeletePageCommand",
    ];

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
                if (!KnownMissingTransactionalRequest.Contains(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty($"New mutating commands (ICommand) must implement ITransactionalRequest. Fix known violations by removing from KnownMissingTransactionalRequest: {string.Join(", ", violations)}");
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
                if (!KnownMissingWorkspaceRequest.Contains(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty($"New commands with WorkspaceId must implement IWorkspaceRequest. Fix known violations by removing from KnownMissingWorkspaceRequest: {string.Join(", ", violations)}");
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
                if (!KnownMissingRequirePermission.Contains(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty($"New Create/Update/Delete/Archive/Restore/Invite/Remove/Assign commands must implement IRequirePermission. Fix known violations by removing from KnownMissingRequirePermission: {string.Join(", ", violations)}");
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
                if (!KnownMissingWorkspaceRequest.Contains(name))
                    violations.Add($"{name}: {Path.GetFileName(file)}");
            }
        }

        violations.Should().BeEmpty($"Commands with ITransactionalRequest and WorkspaceId must also implement IWorkspaceRequest. Fix known violations by removing from KnownMissingWorkspaceRequest: {string.Join(", ", violations)}");
    }
}
