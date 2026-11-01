using System.Runtime.CompilerServices;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CreateApprovalRequest;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ArchiveBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.MoveBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.DeleteBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.MoveBoardToSpace;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RestoreBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UnarchiveBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardMemberRole;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoardVisibility;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.SetDefaultView;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CreateForm;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.CreateLabel;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.UpdateLabel;
using Notrelix.Application.Features.WorkManagement.Relations.Commands.CreateBoardRelation;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardTemplate;
using Notrelix.Application.Features.WorkManagement.Views.Commands.CreateSavedFilter;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Governance;

/// <summary>
/// Managing a Board — settings and title, visibility, moving it, archiving,
/// deleting or restoring it, and its members — needs Manager; Editors only edit
/// content. A Guest is capped at Editor, so a Guest never manages a Board, also
/// on an unplaced Workspace-visible Board. Content-structure commands (groups,
/// labels, the default view, saved filters, forms, relations, approval
/// requests) stay Editor actions by declaring the content action they perform.
/// </summary>
public sealed class BoardManagementLevelTests
{
    private const string Board = "work-management.board";
    private const string Group = "work-management.board-group";
    private const string Label = "work-management.label";

    private static readonly AccessPolicyEngine Engine = new();
    private static readonly Guid BoardId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");

    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    private static readonly RequestDescriptor Descriptor = new(
        typeof(Probe), ApplicationRequestKind.Query, ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource, ApplicationDataAccessKind.Write,
        new AccessRequirements(true, false, false, false), false, false);

    public static TheoryData<Type, string> ManagementCommands() => new()
    {
        { typeof(UpdateBoardCommand), Board },
        { typeof(UpdateBoardVisibilityCommand), Board },
        { typeof(MoveBoardToSpaceCommand), Board },
        { typeof(ArchiveBoardCommand), Board },
        { typeof(UnarchiveBoardCommand), Board },
        { typeof(DeleteBoardCommand), Board },
        { typeof(RestoreBoardCommand), Board },
        { typeof(AddBoardMemberCommand), Board },
        { typeof(RemoveBoardMemberCommand), Board },
        { typeof(UpdateBoardMemberRoleCommand), Board },
        { typeof(CreateBoardTemplateCommand), Board },
    };

    public static TheoryData<Type, string> ContentCommands() => new()
    {
        { typeof(CreateBoardGroupCommand), Board },
        { typeof(DuplicateBoardGroupCommand), Group },
        { typeof(UpdateBoardGroupCommand), Group },
        { typeof(MoveBoardGroupCommand), Group },
        { typeof(ArchiveBoardGroupCommand), Group },
        { typeof(UnarchiveBoardGroupCommand), Group },
        { typeof(DeleteBoardGroupCommand), Group },
        { typeof(CreateLabelCommand), Board },
        { typeof(UpdateLabelCommand), Label },
        { typeof(DeleteLabelCommand), Label },
        { typeof(SetDefaultViewCommand), Board },
        { typeof(CreateSavedFilterCommand), Board },
        { typeof(CreateFormCommand), Board },
        { typeof(CreateBoardRelationCommand), Board },
        { typeof(CreateApprovalRequestCommand), Board },
    };

    [Theory]
    [MemberData(nameof(ManagementCommands))]
    public void ManagingTheBoard_NeedsManager(Type command, string kind)
    {
        var action = ActionOf(command);

        action.Should().Be(PermissionAction.ManageBoard);
        Decide(action, kind, Editor()).Should().Be(AccessDecisionKind.Forbidden, $"an Editor cannot {command.Name}");
        Decide(action, kind, UnplacedMember()).Should().Be(AccessDecisionKind.Forbidden,
            "the Workspace default of an unplaced board is Editor, not Manager");
        Decide(action, kind, GuestWithManagerGrant()).Should().Be(AccessDecisionKind.Forbidden, "a Guest is capped at Viewer");
        Decide(action, kind, Manager()).Should().Be(AccessDecisionKind.Allowed);
    }

    [Theory]
    [MemberData(nameof(ContentCommands))]
    public void EditingContentStructure_StaysAnEditorAction(Type command, string kind)
    {
        var action = ActionOf(command);

        action.Should().NotBe(PermissionAction.ManageBoard, $"{command.Name} edits content");
        Decide(action, kind, Editor()).Should().Be(AccessDecisionKind.Allowed);
        Decide(action, kind, UnplacedMember()).Should().Be(AccessDecisionKind.Allowed);
        Decide(action, kind, GuestWithManagerGrant()).Should().Be(AccessDecisionKind.Forbidden, "a Guest is view-only (GOV-030)");
        Decide(action, kind, Commenter()).Should().Be(AccessDecisionKind.Forbidden);
    }

    [Fact]
    public void PageEditingUnderTheSharedAction_StaysAnEditorAction()
    {
        // Page edit commands (UpdatePage, CreateBlock, MovePage, ...) declare
        // ManageBoard on a documents.page resource; the Manager requirement is
        // about managing Boards only.
        Decide(PermissionAction.ManageBoard, "documents.page", Editor() with { ParentBoardId = null }).Should().Be(AccessDecisionKind.Allowed);
    }

    private static PermissionAction ActionOf(Type command) =>
        ((IRequirePermission)RuntimeHelpers.GetUninitializedObject(command)).Action;

    private static AccessDecisionKind Decide(PermissionAction action, string kind, AccessFacts facts)
    {
        var child = kind != Board && kind != "documents.page";
        var decided = child ? facts with { ParentBoardId = BoardId, IsBoardChild = true } : facts;
        return Engine.Evaluate(Descriptor, Context, decided, new Probe(action, kind)).Kind;
    }

    private static AccessFacts Editor() => Facts("Member", audience: "Private", explicitRank: 3);
    private static AccessFacts Commenter() => Facts("Member", audience: "Private", explicitRank: 2);
    private static AccessFacts Manager() => Facts("Member", audience: "Private", explicitRank: 4);
    private static AccessFacts UnplacedMember() => Facts("Member");
    private static AccessFacts GuestWithManagerGrant() => Facts("Guest", explicitRank: 4);

    private static AccessFacts Facts(string role, string audience = "Workspace", int? explicitRank = null) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: role,
        ResourceExists: true,
        ResourceAudience: audience,
        ResourceMemberRole: null,
        HasExplicitResourcePermission: explicitRank is not null,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null);

    private sealed record Probe(PermissionAction Action, string Kind) : IRequirePermission
    {
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create(Kind), Guid.Parse("00000000-0000-0000-0000-0000000000c1"));
    }
}
