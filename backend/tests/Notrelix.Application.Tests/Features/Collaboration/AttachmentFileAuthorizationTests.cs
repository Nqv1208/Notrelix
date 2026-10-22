using Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment;
using Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachmentContent;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Collaboration;

/// <summary>
/// Direct policy-engine proof for the attachment file upload and download requests (NRX-003):
/// uploading needs the same Editor level as registering a link, downloading needs the same
/// Viewer level as listing attachments, and a caller with no active membership fact is refused.
/// </summary>
public sealed class AttachmentFileAuthorizationTests
{
    private static readonly AccessPolicyEngine Engine = new();
    private static readonly Guid BoardId = Guid.Parse("00000000-0000-0000-0000-0000000000b0");
    private static readonly Guid ItemId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private static readonly ExecutionContextSnapshot Context = new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
        ApplicationPrincipalKind.Authenticated, ApplicationScopeKind.Resource, "test");

    private static UploadBoardItemAttachmentCommand Upload() =>
        new(ItemId, "a.png", Stream.Null);

    private static AccessDecisionKind Evaluate<T>(T request, AccessFacts facts) where T : notnull =>
        Engine.Evaluate(RequestDescriptorValidator.Create(typeof(T)), Context, facts, request).Kind;

    [Fact]
    public void UploadAndLinkRegistration_ShareTheSameAction()
    {
        Upload().Action.Should().Be(PermissionAction.UpdateItem);
        new CreateBoardItemAttachmentCommand(ItemId, "a", "https://example.com", null, null, null).Action
            .Should().Be(Upload().Action);
        new GetBoardItemAttachmentContentQuery(ItemId, Guid.NewGuid()).Action.Should().Be(PermissionAction.ViewBoard);
    }

    [Fact]
    public void Upload_NeedsEditor_ButDownloadOnlyNeedsViewer()
    {
        var viewer = Facts(explicitRank: 1);
        var editor = Facts(explicitRank: 3);

        Evaluate(Upload(), viewer).Should().Be(AccessDecisionKind.Forbidden, "a visible-but-not-editable item refuses the upload");
        Evaluate(Upload(), editor).Should().Be(AccessDecisionKind.Allowed);
        Evaluate(new GetBoardItemAttachmentContentQuery(ItemId, Guid.NewGuid()), viewer)
            .Should().Be(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void CallerWithoutAnyActiveMembershipFact_IsNotAllowed()
    {
        // Facts for a suspended or removed member carry no active role and no explicit grant
        // (the SQL facts provider is covered by integration tests); the engine must then refuse.
        var noMembership = Facts(explicitRank: 4) with
        {
            WorkspaceMemberRole = null,
            ResourceMemberRole = null,
            HasExplicitResourcePermission = false,
            ActiveResourcePermissionRank = null,
        };

        Evaluate(Upload(), noMembership).Should().NotBe(AccessDecisionKind.Allowed);
        Evaluate(new GetBoardItemAttachmentContentQuery(ItemId, Guid.NewGuid()), noMembership)
            .Should().NotBe(AccessDecisionKind.Allowed);
    }

    [Fact]
    public void ItemOfAnotherWorkspaceOrMissingBoard_IsNotFound()
    {
        var orphan = Facts(explicitRank: 4) with { ParentBoardId = null };

        Evaluate(Upload(), orphan).Should().Be(AccessDecisionKind.NotFound);
    }

    private static AccessFacts Facts(int explicitRank) => new(
        UserExists: true,
        EmailVerified: true,
        AccountExists: true,
        AccountMemberRole: "Member",
        WorkspaceExists: true,
        WorkspaceMemberRole: "Member",
        ResourceExists: true,
        ResourceAudience: "Private",
        ResourceMemberRole: null,
        HasExplicitResourcePermission: true,
        PermissionRules: [],
        SubscriptionRequirementSatisfied: true,
        FeatureEnabled: true,
        ActiveResourcePermissionRank: explicitRank,
        TargetPermissionRank: null)
    {
        ParentBoardId = BoardId,
        IsBoardChild = true,
    };
}
