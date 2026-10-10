using System.Runtime.CompilerServices;
using Notrelix.Application.Common.Requests.Security;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Architecture.Tests.WorkManagement;

/// <summary>
/// GOV-DEBT-001 (SPEC WMREQ155, PLAN WM-V3-GATE-002) — disposition gates for the
/// Workspace &amp; Governance handoff debts that P3 inherits.
///
/// WG-DEBT-006: AccessFactsQuery performs a composite read of Work Board
/// persistence. Accepted for P3 as owned Governance debt; this gate freezes the set
/// of Work tables it reads so the read cannot silently grow.
///
/// WG-DEBT-007: PermissionAction.ManageBoard is broader than the Work command
/// matrix. Accepted for P3 as owned debt; this gate pins the action of every active
/// P3 request, so a new request cannot pick up ManageBoard (or any action) without
/// an explicit, reviewed matrix change. Removal trigger: Governance introduces
/// finer Work actions.
/// </summary>
public sealed class WorkPermissionActionMatrixArchitectureTests
{
    private static readonly HashSet<string> AccessFactsWorkTablesBaseline = new(StringComparer.Ordinal)
    {
        "work.boards",
        "work.board_members",
    };

    /// <summary>Pinned action of every active (non-retired) P3 request.</summary>
    private static readonly IReadOnlyDictionary<string, PermissionAction> Matrix = new Dictionary<string, PermissionAction>(StringComparer.Ordinal)
    {
        ["Boards.GetBoardQuery"] = PermissionAction.ViewBoard,
        ["Boards.GetFullBoardQuery"] = PermissionAction.ViewBoard,
        ["Boards.GetBoardMembersQuery"] = PermissionAction.ViewBoard,
        ["Boards.GetBoardsQuery"] = PermissionAction.ViewWorkspace,
        ["Boards.GetBoardsBySlugQuery"] = PermissionAction.ViewWorkspace,
        ["Boards.CreateBoardInWorkspaceCommand"] = PermissionAction.CreateBoard,
        ["Boards.UpdateBoardCommand"] = PermissionAction.ManageBoard,
        ["Boards.UpdateBoardVisibilityCommand"] = PermissionAction.ManageBoard,
        ["Boards.ArchiveBoardCommand"] = PermissionAction.ManageBoard,
        ["Boards.UnarchiveBoardCommand"] = PermissionAction.ManageBoard,
        ["Boards.DeleteBoardCommand"] = PermissionAction.ManageBoard,
        ["Boards.RestoreBoardCommand"] = PermissionAction.ManageBoard,
        ["Boards.AddBoardMemberCommand"] = PermissionAction.ManageBoard,
        ["Boards.RemoveBoardMemberCommand"] = PermissionAction.ManageBoard,
        ["Boards.UpdateBoardMemberRoleCommand"] = PermissionAction.ManageBoard,

        ["BoardItems.GetBoardItemQuery"] = PermissionAction.ViewBoard,
        ["BoardItems.GetBoardItemsQuery"] = PermissionAction.ViewBoard,
        ["BoardItems.GetMyBoardItemsQuery"] = PermissionAction.ViewWorkspace,
        ["BoardItems.CreateBoardItemCommand"] = PermissionAction.CreateItem,
        ["BoardItems.DuplicateBoardItemCommand"] = PermissionAction.CreateItem,
        ["BoardItems.MoveBoardItemCommand"] = PermissionAction.MoveItem,
        ["BoardItems.AssignBoardItemMemberCommand"] = PermissionAction.AssignItem,
        ["BoardItems.UnassignBoardItemMemberCommand"] = PermissionAction.AssignItem,
        ["BoardItems.UpdateBoardItemCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.UpdateBoardItemFieldValueCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.UpdateBoardItemFieldValuesCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.ClearFieldValueCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.UpdateBoardItemStatusCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.SetBoardItemDueDateCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.CompleteBoardItemCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.ArchiveBoardItemCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.UnarchiveBoardItemCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.DeleteBoardItemCommand"] = PermissionAction.UpdateItem,
        ["BoardItems.RestoreBoardItemCommand"] = PermissionAction.UpdateItem,

        ["BoardFields.CreateBoardFieldCommand"] = PermissionAction.CreateField,
        ["BoardFields.UpdateBoardFieldCommand"] = PermissionAction.UpdateField,
        ["BoardFields.DeleteBoardFieldCommand"] = PermissionAction.DeleteField,
        ["BoardFields.MoveBoardFieldCommand"] = PermissionAction.UpdateField,
        ["BoardFields.AddFieldOptionCommand"] = PermissionAction.UpdateField,
        ["BoardFields.UpdateFieldOptionCommand"] = PermissionAction.UpdateField,
        ["BoardFields.RemoveFieldOptionCommand"] = PermissionAction.UpdateField,
        ["BoardFields.MoveFieldOptionCommand"] = PermissionAction.UpdateField,

        ["BoardGroups.CreateBoardGroupCommand"] = PermissionAction.ManageBoard,
        ["BoardGroups.UpdateBoardGroupCommand"] = PermissionAction.ManageBoard,
        ["BoardGroups.MoveBoardGroupCommand"] = PermissionAction.ManageBoard,
        ["BoardGroups.DuplicateBoardGroupCommand"] = PermissionAction.ManageBoard,
        ["BoardGroups.ArchiveBoardGroupCommand"] = PermissionAction.ManageBoard,
        ["BoardGroups.UnarchiveBoardGroupCommand"] = PermissionAction.ManageBoard,
        ["BoardGroups.DeleteBoardGroupCommand"] = PermissionAction.ManageBoard,

        ["Checklists.GetChecklistsQuery"] = PermissionAction.ViewBoard,
        ["Checklists.CreateChecklistCommand"] = PermissionAction.UpdateItem,
        ["Checklists.UpdateChecklistCommand"] = PermissionAction.UpdateItem,
        ["Checklists.MoveChecklistCommand"] = PermissionAction.UpdateItem,
        ["Checklists.DeleteChecklistCommand"] = PermissionAction.UpdateItem,
        ["Checklists.CreateChecklistItemCommand"] = PermissionAction.UpdateItem,
        ["Checklists.MoveChecklistItemCommand"] = PermissionAction.UpdateItem,
        ["Checklists.UpdateChecklistItemCommand"] = PermissionAction.UpdateItem,
        ["Checklists.ToggleChecklistItemCommand"] = PermissionAction.UpdateItem,
        ["Checklists.DeleteChecklistItemCommand"] = PermissionAction.UpdateItem,

        ["BoardSchema.GetBoardSchemaQuery"] = PermissionAction.ViewBoard,

        ["Labels.GetLabelsQuery"] = PermissionAction.ViewBoard,
        ["Labels.CreateLabelCommand"] = PermissionAction.ManageBoard,
        ["Labels.UpdateLabelCommand"] = PermissionAction.ManageBoard,
        ["Labels.DeleteLabelCommand"] = PermissionAction.ManageBoard,
        ["Labels.AddLabelToBoardItemCommand"] = PermissionAction.UpdateItem,
        ["Labels.RemoveLabelFromBoardItemCommand"] = PermissionAction.UpdateItem,
    };

    [Fact]
    public void AccessFactsQuery_ReadsOnlyTheRecordedWorkTables()
    {
        var source = File.ReadAllText(Path.Combine(FindSrcRoot(), "Notrelix.Infrastructure", "Data", "Authz", "AccessFactsQuery.cs"));
        var workTables = Regex.Matches(source, @"\bwork\.[a-z_]+").Select(m => m.Value).ToHashSet(StringComparer.Ordinal);

        workTables.Should().NotBeEmpty("the scan must inspect the real query (NRX-016)");
        workTables.Should().BeSubsetOf(AccessFactsWorkTablesBaseline,
            "WG-DEBT-006: the Governance access-facts read of Work persistence must not grow without a recorded disposition");
    }

    [Fact]
    public void EveryActiveP3Request_DeclaresItsPinnedPermissionAction()
    {
        var active = WorkP3ManifestArchitectureTests.P3Requests
            .Where(e => e.Value.Disposition != WorkP3ManifestArchitectureTests.Disposition.Retire)
            .Select(e => e.Key)
            .ToHashSet(StringComparer.Ordinal);

        active.Should().BeEquivalentTo(Matrix.Keys,
            "every active P3 request needs exactly one pinned PermissionAction (WG-DEBT-007)");

        var types = typeof(IRequirePermission).Assembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("Notrelix.Application.Features.WorkManagement.", StringComparison.Ordinal) == true)
            .ToLookup(t => $"{t.Namespace!.Split('.')[4]}.{t.Name}", StringComparer.Ordinal);

        var mismatches = new List<string>();
        foreach (var (key, expected) in Matrix)
        {
            var type = types[key].Single();
            if (!typeof(IRequirePermission).IsAssignableFrom(type))
            {
                mismatches.Add($"{key}: does not declare IRequirePermission");
                continue;
            }

            var actual = ((IRequirePermission)RuntimeHelpers.GetUninitializedObject(type)).Action;
            if (actual != expected)
            {
                mismatches.Add($"{key}: declares {actual}, matrix pins {expected}");
            }
        }

        mismatches.Should().BeEmpty(
            "a Work request's PermissionAction is a reviewed Governance contract; change the matrix deliberately (WG-DEBT-007)");
    }

    private static string FindSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "Notrelix.Application")))
        {
            dir = dir.Parent;
        }

        return dir is null
            ? throw new DirectoryNotFoundException("Could not locate backend/src root from test directory")
            : Path.Combine(dir.FullName, "src");
    }
}
