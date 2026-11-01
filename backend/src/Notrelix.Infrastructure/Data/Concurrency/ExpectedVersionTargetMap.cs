using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.MoveBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveChildItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.ToggleChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.ApproveApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.DeleteApprovalRequest;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RejectApprovalRequest;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;
using Notrelix.Application.Features.WorkManagement.Views.Commands.DeleteSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.RenameSavedFilter;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterFilters;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterGroup;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterSorts;
using Notrelix.Application.Features.WorkManagement.Views.Commands.UpdateSavedFilterVisibility;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UnarchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UpdateWorkspaceProfile;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.AddSpaceMember;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceType;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceVisibility;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RenameSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.UnarchiveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.UpdateSpaceDescription;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.MoveBoardToSpace;
using Notrelix.Application.Features.Documents.Pages.Commands.MovePage;
using Notrelix.Application.Features.Documents.Pages.Commands.MovePageToSpace;
using Notrelix.Application.Features.Documents.Pages.Commands.UpdatePage;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceMemberRole;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.MoveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RemoveSpaceMember;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RenameTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.UpdateTeamDescription;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ArchiveTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.UnarchiveTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.DeleteTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RemoveTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.ChangeTeamMemberRole;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberJobTitle;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ChangeInvitationRole;
using Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRole;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMyJobTitle;

namespace Notrelix.Infrastructure.Data.Concurrency;

/// <summary>
/// Sole bridge from a concrete <see cref="IExpectedVersionRequest"/> type to the
/// aggregate CLR type its expected-version constraint protects, plus the
/// resource kind the request must declare. Immutable after startup; no I/O.
/// A versioned request without an entry here fails closed at binding time and is
/// rejected by the executable completeness test in ExpectedVersionArchitectureTests.
/// </summary>
public sealed class ExpectedVersionTargetMap
{
    /// <param name="OwnsResource">
    /// For a child resource whose concurrency authority is its owning aggregate
    /// (SPEC WM-V3-DEC-007, e.g. a ChecklistItem protected by its Checklist): selects
    /// the tracked aggregate that owns the declared resource id. Null means the
    /// declared resource id is the aggregate id.
    /// </param>
    public sealed record TargetDefinition(string ExpectedResourceKind, Type AggregateType, Func<AggregateRoot, Guid, bool>? OwnsResource = null)
    {
        public bool Matches(AggregateRoot aggregate, Guid resourceId) =>
            OwnsResource?.Invoke(aggregate, resourceId) ?? aggregate.Id == resourceId;
    }

    private readonly IReadOnlyDictionary<Type, TargetDefinition> _targets;

    public static ExpectedVersionTargetMap Default { get; } = new(BuildDefault());

    public ExpectedVersionTargetMap(IReadOnlyDictionary<Type, TargetDefinition> targets)
    {
        foreach (var (requestType, definition) in targets)
        {
            if (!typeof(AggregateRoot).IsAssignableFrom(definition.AggregateType))
            {
                throw new InvalidOperationException(
                    $"ExpectedVersion target for {requestType.Name} must derive from AggregateRoot " +
                    $"(got {definition.AggregateType.FullName}).");
            }
        }

        _targets = new Dictionary<Type, TargetDefinition>(targets);
    }

    public TargetDefinition Resolve(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        return _targets.TryGetValue(requestType, out var definition)
            ? definition
            : throw new InvalidOperationException(
                $"No expected-version target mapping is registered for {requestType.FullName}.");
    }

    public bool Contains(Type requestType) => _targets.ContainsKey(requestType);

    public IReadOnlyCollection<KeyValuePair<Type, TargetDefinition>> Entries => [.. _targets];

    private const string ApprovalRequestKind = "work-management.approval-request";
    private const string BoardKind = "work-management.board";
    private const string BoardFieldKind = "work-management.board-field";
    private const string BoardGroupKind = "work-management.board-group";
    private const string BoardItemKind = "work-management.board-item";
    private const string ChecklistKind = "work-management.checklist";
    private const string ChecklistItemKind = "work-management.checklist-item";

    // A method, not a static field: Default is initialized before later static fields.
    private static bool ChecklistOwnsItem(AggregateRoot aggregate, Guid itemId) =>
        aggregate is Checklist checklist && checklist.Items.Any(item => item.Id == itemId);
    private const string FormKind = "work-management.form";
    private const string SavedFilterKind = "work-management.saved-filter";
    private const string WorkspaceKind = "workspaces.workspace";
    private const string SpaceKind = "workspaces.space";
    private const string TeamKind = "workspaces.team";
    private const string WorkspaceMemberKind = "workspaces.workspace-member";
    private const string WorkspaceInvitationKind = "workspaces.workspace-invitation";

    // The member write loads exactly one WorkspaceMember: another member is addressed by user id,
    // the caller's own by its Workspace id (the caller's user id is not part of the request).
    private static bool IsMemberOfUser(AggregateRoot aggregate, Guid userId) =>
        aggregate is WorkspaceMember member && member.UserId == userId;
    private static bool IsMemberOfWorkspace(AggregateRoot aggregate, Guid workspaceId) =>
        aggregate is WorkspaceMember member && member.WorkspaceId == workspaceId;

    private static Dictionary<Type, TargetDefinition> BuildDefault() => new(new Dictionary<Type, TargetDefinition>
    {
        // Work Management — approvals
        [typeof(ApproveApprovalRequestCommand)] = new(ApprovalRequestKind, typeof(ApprovalRequest)),
        [typeof(RejectApprovalRequestCommand)] = new(ApprovalRequestKind, typeof(ApprovalRequest)),
        [typeof(CancelApprovalRequestCommand)] = new(ApprovalRequestKind, typeof(ApprovalRequest)),
        [typeof(DeleteApprovalRequestCommand)] = new(ApprovalRequestKind, typeof(ApprovalRequest)),

        // Work Management — boards / fields / groups / items
        // (the P3 versioned-request manifest, SPEC WM-V3-DEC-007 / WM-V3-CONC-001)
        [typeof(UpdateBoardCommand)] = new(BoardKind, typeof(Board)),
        [typeof(MoveBoardToSpaceCommand)] = new(BoardKind, typeof(Board)),
        [typeof(UpdateBoardFieldCommand)] = new(BoardFieldKind, typeof(BoardField)),
        [typeof(MoveBoardFieldCommand)] = new(BoardFieldKind, typeof(BoardField)),
        [typeof(AddFieldOptionCommand)] = new(BoardFieldKind, typeof(BoardField)),
        [typeof(MoveFieldOptionCommand)] = new(BoardFieldKind, typeof(BoardField)),
        [typeof(UpdateFieldOptionCommand)] = new(BoardFieldKind, typeof(BoardField)),
        [typeof(RemoveFieldOptionCommand)] = new(BoardFieldKind, typeof(BoardField)),
        [typeof(UpdateBoardGroupCommand)] = new(BoardGroupKind, typeof(BoardGroup)),
        [typeof(MoveBoardGroupCommand)] = new(BoardGroupKind, typeof(BoardGroup)),
        [typeof(DeleteBoardGroupCommand)] = new(BoardGroupKind, typeof(BoardGroup)),
        [typeof(UpdateBoardItemCommand)] = new(BoardItemKind, typeof(BoardItem)),
        [typeof(ArchiveBoardItemCommand)] = new(BoardItemKind, typeof(BoardItem)),
        [typeof(MoveBoardItemCommand)] = new(BoardItemKind, typeof(BoardItem)),
        [typeof(MoveChildItemCommand)] = new(BoardItemKind, typeof(BoardItem)),
        [typeof(UpdateBoardItemFieldValueCommand)] = new(BoardItemKind, typeof(BoardItem)),
        [typeof(UpdateBoardItemFieldValuesCommand)] = new(BoardItemKind, typeof(BoardItem)),
        [typeof(ClearFieldValueCommand)] = new(BoardItemKind, typeof(BoardItem)),
        // Work Management — checklists; item mutations are guarded by the owning Checklist
        [typeof(UpdateChecklistCommand)] = new(ChecklistKind, typeof(Checklist)),
        [typeof(MoveChecklistCommand)] = new(ChecklistKind, typeof(Checklist)),
        [typeof(CreateChecklistItemCommand)] = new(ChecklistKind, typeof(Checklist)),
        [typeof(MoveChecklistItemCommand)] = new(ChecklistItemKind, typeof(Checklist), ChecklistOwnsItem),
        [typeof(UpdateChecklistItemCommand)] = new(ChecklistItemKind, typeof(Checklist), ChecklistOwnsItem),
        [typeof(ToggleChecklistItemCommand)] = new(ChecklistItemKind, typeof(Checklist), ChecklistOwnsItem),
        [typeof(DeleteChecklistItemCommand)] = new(ChecklistItemKind, typeof(Checklist), ChecklistOwnsItem),

        // Work Management — forms
        [typeof(UpdateFormDetailsCommand)] = new(FormKind, typeof(Form)),
        [typeof(PublishFormCommand)] = new(FormKind, typeof(Form)),
        [typeof(CloseFormCommand)] = new(FormKind, typeof(Form)),
        [typeof(DeleteFormCommand)] = new(FormKind, typeof(Form)),

        // Work Management — saved filters (board views)
        [typeof(RenameSavedFilterCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(DeleteSavedFilterCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(UpdateSavedFilterFiltersCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(UpdateSavedFilterSortsCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(UpdateSavedFilterVisibilityCommand)] = new(SavedFilterKind, typeof(SavedFilter)),
        [typeof(UpdateSavedFilterGroupCommand)] = new(SavedFilterKind, typeof(SavedFilter)),

        // Workspaces
        [typeof(ArchiveWorkspaceCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(UnarchiveWorkspaceCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(RestoreWorkspaceCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(TransferOwnershipCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(DeleteWorkspaceCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(UpdateWorkspaceProfileCommand)] = new(WorkspaceKind, typeof(Workspace)),
        [typeof(UpdateWorkspaceSettingsCommand)] = new(WorkspaceKind, typeof(Workspace)),

        // Documents — Space placement of a root page (docs/product/workspaces.md §79)
        [typeof(MovePageToSpaceCommand)] = new("documents.page", typeof(Page)),
        // Documents — page rename and move under another parent: the page version the editor read.
        [typeof(UpdatePageCommand)] = new("documents.page", typeof(Page)),
        [typeof(MovePageCommand)] = new("documents.page", typeof(Page)),

        // Workspaces — Space hierarchy and membership (WSP-021, WSP-024)
        [typeof(MoveSpaceCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(RenameSpaceCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(UpdateSpaceDescriptionCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(ChangeSpaceTypeCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(ChangeSpaceVisibilityCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(ArchiveSpaceCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(UnarchiveSpaceCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(AddSpaceMemberCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(RemoveSpaceMemberCommand)] = new(SpaceKind, typeof(Space)),
        [typeof(ChangeSpaceMemberRoleCommand)] = new(SpaceKind, typeof(Space)),

        // Workspaces — member profile (job title, D15)
        [typeof(UpdateMemberJobTitleCommand)] = new(WorkspaceMemberKind, typeof(WorkspaceMember), IsMemberOfUser),
        [typeof(UpdateMyJobTitleCommand)] = new(WorkspaceMemberKind, typeof(WorkspaceMember), IsMemberOfWorkspace),

        // Workspaces — membership writes (LD-07): role, suspend, activate and remove quote the target member's version
        [typeof(UpdateMemberRoleCommand)] = new(WorkspaceMemberKind, typeof(WorkspaceMember), IsMemberOfUser),
        [typeof(SuspendMemberCommand)] = new(WorkspaceMemberKind, typeof(WorkspaceMember), IsMemberOfUser),
        [typeof(ActivateMemberCommand)] = new(WorkspaceMemberKind, typeof(WorkspaceMember), IsMemberOfUser),
        [typeof(RemoveMemberCommand)] = new(WorkspaceMemberKind, typeof(WorkspaceMember), IsMemberOfUser),

        // Workspaces — invitation writes (LD-07): changing the offered role quotes the invitation's version
        [typeof(ChangeInvitationRoleCommand)] = new(WorkspaceInvitationKind, typeof(WorkspaceInvitation)),

        // Workspaces — Team writes (RestoreTeam addresses a deleted Team and stays version-less)
        [typeof(RenameTeamCommand)] = new(TeamKind, typeof(Team)),
        [typeof(UpdateTeamDescriptionCommand)] = new(TeamKind, typeof(Team)),
        [typeof(ArchiveTeamCommand)] = new(TeamKind, typeof(Team)),
        [typeof(UnarchiveTeamCommand)] = new(TeamKind, typeof(Team)),
        [typeof(DeleteTeamCommand)] = new(TeamKind, typeof(Team)),
        [typeof(AddTeamMemberCommand)] = new(TeamKind, typeof(Team)),
        [typeof(RemoveTeamMemberCommand)] = new(TeamKind, typeof(Team)),
        [typeof(ChangeTeamMemberRoleCommand)] = new(TeamKind, typeof(Team)),
    });

    /// <summary>Validates the declared resource kind against the mapped expectation.</summary>
    public bool MatchesKind(Type requestType, ResourceRef resource)
    {
        var definition = Resolve(requestType);
        return string.Equals(definition.ExpectedResourceKind, resource.Kind.Value, StringComparison.Ordinal);
    }
}
