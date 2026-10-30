using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Data.Concurrency;

namespace Notrelix.Architecture.Tests.Pipeline;

/// <summary>
/// IA-TST-EV-ARCH — executable specification of the fail-closed
/// expected-version contract (ADR-006 and
/// <c>backend/docs/architecture/application-model.md</c>).
///
/// A concrete versioned request that is absent from <see cref="ExpectedVersionTargetMap"/>
/// fails this test in CI before it can silently skip concurrency enforcement at runtime.
/// </summary>
public static class ExpectedVersionInventory
{
    /// <summary>The declared ResourceRef.Kind each versioned request must carry.</summary>
    public static readonly IReadOnlyDictionary<string, string> RequestKindBySimpleName =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ApproveApprovalRequestCommand"] = "work-management.approval-request",
            ["CancelApprovalRequestCommand"] = "work-management.approval-request",
            ["DeleteApprovalRequestCommand"] = "work-management.approval-request",
            ["RejectApprovalRequestCommand"] = "work-management.approval-request",
            ["UpdateBoardCommand"] = "work-management.board",
            ["MoveBoardToSpaceCommand"] = "work-management.board",
            ["MovePageToSpaceCommand"] = "documents.page",
            ["UpdateBoardFieldCommand"] = "work-management.board-field",
            ["DeleteBoardGroupCommand"] = "work-management.board-group",
            ["UpdateBoardItemCommand"] = "work-management.board-item",
            ["ArchiveBoardItemCommand"] = "work-management.board-item",
            ["MoveBoardItemCommand"] = "work-management.board-item",
            ["MoveChildItemCommand"] = "work-management.board-item",
            ["UpdateBoardItemFieldValueCommand"] = "work-management.board-item",
            ["UpdateBoardItemFieldValuesCommand"] = "work-management.board-item",
            ["ClearFieldValueCommand"] = "work-management.board-item",
            ["MoveBoardFieldCommand"] = "work-management.board-field",
            ["AddFieldOptionCommand"] = "work-management.board-field",
            ["MoveFieldOptionCommand"] = "work-management.board-field",
            ["UpdateFieldOptionCommand"] = "work-management.board-field",
            ["RemoveFieldOptionCommand"] = "work-management.board-field",
            ["UpdateBoardGroupCommand"] = "work-management.board-group",
            ["MoveBoardGroupCommand"] = "work-management.board-group",
            ["UpdateChecklistCommand"] = "work-management.checklist",
            ["MoveChecklistCommand"] = "work-management.checklist",
            ["CreateChecklistItemCommand"] = "work-management.checklist",
            ["MoveChecklistItemCommand"] = "work-management.checklist-item",
            ["UpdateChecklistItemCommand"] = "work-management.checklist-item",
            ["ToggleChecklistItemCommand"] = "work-management.checklist-item",
            ["DeleteChecklistItemCommand"] = "work-management.checklist-item",
            ["CloseFormCommand"] = "work-management.form",
            ["DeleteFormCommand"] = "work-management.form",
            ["PublishFormCommand"] = "work-management.form",
            ["UpdateFormDetailsCommand"] = "work-management.form",
            ["DeleteSavedFilterCommand"] = "work-management.saved-filter",
            ["RenameSavedFilterCommand"] = "work-management.saved-filter",
            ["UpdateSavedFilterFiltersCommand"] = "work-management.saved-filter",
            ["UpdateSavedFilterGroupCommand"] = "work-management.saved-filter",
            ["UpdateSavedFilterSortsCommand"] = "work-management.saved-filter",
            ["UpdateSavedFilterVisibilityCommand"] = "work-management.saved-filter",
            ["ArchiveWorkspaceCommand"] = "workspaces.workspace",
            ["DeleteWorkspaceCommand"] = "workspaces.workspace",
            ["RestoreWorkspaceCommand"] = "workspaces.workspace",
            ["TransferOwnershipCommand"] = "workspaces.workspace",
            ["UnarchiveWorkspaceCommand"] = "workspaces.workspace",
            ["UpdateWorkspaceProfileCommand"] = "workspaces.workspace",
            ["UpdateWorkspaceSettingsCommand"] = "workspaces.workspace",
            ["MoveSpaceCommand"] = "workspaces.space",
            ["AddSpaceMemberCommand"] = "workspaces.space",
            ["RemoveSpaceMemberCommand"] = "workspaces.space",
            ["ChangeSpaceMemberRoleCommand"] = "workspaces.space",
            ["RenameTeamCommand"] = "workspaces.team",
            ["UpdateTeamDescriptionCommand"] = "workspaces.team",
            ["ArchiveTeamCommand"] = "workspaces.team",
            ["UnarchiveTeamCommand"] = "workspaces.team",
            ["DeleteTeamCommand"] = "workspaces.team",
            ["AddTeamMemberCommand"] = "workspaces.team",
            ["RemoveTeamMemberCommand"] = "workspaces.team",
            ["ChangeTeamMemberRoleCommand"] = "workspaces.team",
            ["UpdateMemberJobTitleCommand"] = "workspaces.workspace-member",
            ["UpdateMyJobTitleCommand"] = "workspaces.workspace-member",
        };

    public static IReadOnlyCollection<Type> DiscoverConcreteVersionedRequests() =>
        typeof(IExpectedVersionRequest).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                && typeof(IExpectedVersionRequest).IsAssignableFrom(type))
            .ToArray();
}

public sealed class ExpectedVersionArchitectureTests
{
    [Fact]
    public void No_ConcurrencyBehavior_Exists_InApplicationAssembly()
    {
        var offenders = typeof(IExpectedVersionRequest).Assembly.GetTypes()
            .Where(type => type.Name.Contains("ConcurrencyBehavior", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToArray();

        offenders.Should().BeEmpty(
            "concurrency is persistence-level optimistic concurrency inside the data session, never a pipeline behavior");
    }

    [Fact]
    public void EveryConcreteVersionedRequest_IsAWriteRequest()
    {
        var offenders = ExpectedVersionInventory.DiscoverConcreteVersionedRequests()
            .Where(type => !typeof(IWriteRequest).IsAssignableFrom(type))
            .Select(type => type.Name)
            .ToArray();

        offenders.Should().BeEmpty("expected-version guards only make sense on transactional write requests");
    }

    [Fact]
    public void EveryConcreteVersionedRequest_HasExactlyOneTargetMapEntry()
    {
        var discovered = ExpectedVersionInventory.DiscoverConcreteVersionedRequests();
        var mapped = ExpectedVersionTargetMap.Default.Entries;

        var missing = discovered
            .Where(type => mapped.All(entry => entry.Key != type))
            .Select(type => type.FullName)
            .ToArray();
        var stale = mapped
            .Where(entry => !discovered.Contains(entry.Key))
            .Select(entry => entry.Key.FullName)
            .ToArray();

        missing.Should().BeEmpty(
            "every concrete IExpectedVersionRequest must be registered in ExpectedVersionTargetMap — " +
            "an unmapped request fails closed at binding time and blocks CI here");
        stale.Should().BeEmpty("target-map entries must reference live versioned requests only");
    }

    [Fact]
    public void EveryMappedEntry_DeclaresTheDocumentedResourceKind()
    {
        var offenders = ExpectedVersionTargetMap.Default.Entries
            .Where(entry => ExpectedVersionInventory.RequestKindBySimpleName.TryGetValue(
                    entry.Key.Name, out var expectedKind)
                && !string.Equals(entry.Value.ExpectedResourceKind, expectedKind, StringComparison.Ordinal))
            .Select(entry => $"{entry.Key.Name}: map='{entry.Value.ExpectedResourceKind}' inventory='{ExpectedVersionInventory.RequestKindBySimpleName[entry.Key.Name]}'")
            .ToArray();

        offenders.Should().BeEmpty("the resource kind is an independent validation dimension at binding time");
    }

    [Fact]
    public void EveryMappedTarget_DerivesFromAggregateRoot()
    {
        var offenders = ExpectedVersionTargetMap.Default.Entries
            .Where(entry => !typeof(AggregateRoot).IsAssignableFrom(entry.Value.AggregateType))
            .Select(entry => $"{entry.Key.Name} -> {entry.Value.AggregateType.Name}")
            .ToArray();

        offenders.Should().BeEmpty(
            "expected-version binding tracks aggregate roots with a configured concurrency version token");
    }

    [Fact]
    public void VersionedRequestCount_MatchesDocumentedInventory()
    {
        var discovered = ExpectedVersionInventory.DiscoverConcreteVersionedRequests();

        discovered.Should().HaveCount(ExpectedVersionInventory.RequestKindBySimpleName.Count,
            "a new versioned request requires an inventory row AND a target-map entry; " +
            "update both together with the expected-version contract in backend/docs/architecture/application-model.md");
    }
}
