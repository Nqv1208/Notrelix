using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteGuestToResource;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.ResendResourceInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.RevokeResourceInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.Resource;

/// <summary>
/// WSP-026: the issuer resends and revokes their own resource invitation, the
/// resource-invitation event names the resource and inviter, and Admin is no
/// longer invitable. Authorization of the issuer's permission is decided by the
/// pipeline (the request is IRequirePermission on the resource); the handler only
/// selects the actor's own invitation.
/// </summary>
public class ResourceInvitationLifecycleTests : WorkspaceHandlerTestBase
{
    private const string BoardKind = WorkspaceInvitationResourceGrant.BoardKind;
    private const string Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
    private static readonly Guid BoardId = Guid.CreateVersion7();

    private readonly Mock<IOneTimeTokenService> _tokens = new();
    private readonly Mock<ISecretEncryptor> _encryptor = new();
    private readonly Mock<IIntegrationEventCollector> _events = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IGuestResourceInvitationFacts> _facts = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<Notrelix.Application.Features.Workspaces.Public.Members.IMemberEmailDisclosure> _memberEmails = new();
    private readonly List<IIntegrationEvent> _published = [];

    public ResourceInvitationLifecycleTests()
    {
        _events.Setup(e => e.Add(It.IsAny<IIntegrationEvent>())).Callback<IIntegrationEvent>(_published.Add);
        _memberEmails.Setup(m => m.ResolveAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        _tokens.Setup(t => t.Generate(TokenPurpose.WorkspaceInvitation))
            .Returns(new IssuedOneTimeToken("raw-token", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 1));
        _encryptor.Setup(e => e.Protect(It.IsAny<string>(), It.IsAny<string>())).Returns("protected");
        _actors.Setup(a => a.FindAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorSnapshot(TestUserId, "Mai Linh", null));
        _facts.Setup(f => f.ResolveTitleAsync(BoardKind, BoardId, TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Triển khai ERP Q4");
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
    }

    private WorkspaceInvitation ResourceInvitation(Guid? invitedBy = null, Guid? boardId = null) =>
        WorkspaceInvitation.CreateForResource(
            TestAccountId, TestWorkspaceId, "guest@example.com",
            new WorkspaceInvitationResourceGrant(BoardKind, boardId ?? BoardId, "Editor"),
            InvitationTokenHash.Create(Hash), 1, invitedBy ?? TestUserId, TestNow.AddDays(-1));

    private ResendResourceInvitationCommandHandler ResendHandler() => new(
        DbContextMock.Object, RequestContextMock.Object, _actors.Object, _facts.Object, _tokens.Object,
        _encryptor.Object, _events.Object, DateTimeProviderMock.Object, _audit.Object);

    private RevokeResourceInvitationCommandHandler RevokeHandler() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _audit.Object);

    // ── Authorization shape (decided by the pipeline) ─────────────────────────

    [Theory]
    [InlineData("work-management.board", "ManageBoardPermission")]
    [InlineData("documents.page", "ManagePagePermission")]
    public void ResendAndRevoke_AreAuthorizedAsManagePermissionOnTheResource(string kind, string action)
    {
        IRequirePermission resend = new ResendResourceInvitationCommand(kind, BoardId, Guid.NewGuid());
        IRequirePermission revoke = new RevokeResourceInvitationCommand(kind, BoardId, Guid.NewGuid());

        resend.Action.ToString().Should().Be(action);
        revoke.Action.ToString().Should().Be(action);
        resend.Resource!.ResourceId.Should().Be(BoardId);
        revoke.Resource!.Kind.Value.Should().Be(kind);
    }

    // ── Resend ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resend_ByTheIssuer_RotatesTheTokenAndQueuesAResourceInvitationEvent()
    {
        var invitation = ResourceInvitation();
        SetupInvitations(invitation);
        var generation = invitation.TokenGeneration;

        var result = await ResendHandler().Handle(
            new ResendResourceInvitationCommand(BoardKind, BoardId, invitation.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        invitation.TokenGeneration.Should().Be(generation + 1);
        invitation.Token.Value.Should().NotBe(Hash);
        var published = _published.Should().ContainSingle().Which
            .Should().BeOfType<WorkspaceInvitationDeliveryRequestedIntegrationEventV1>().Subject;
        published.ResourceKind.Should().Be(BoardKind);
        published.ResourceId.Should().Be(BoardId);
        published.ResourceName.Should().Be("Triển khai ERP Q4");
        published.InviterName.Should().Be("Mai Linh");
        published.TokenGeneration.Should().Be(generation + 1);
    }

    [Fact]
    public async Task Resend_OfAnotherMembersInvitation_IsNotFoundAndChangesNothing()
    {
        var invitation = ResourceInvitation(invitedBy: Guid.CreateVersion7());
        SetupInvitations(invitation);
        var generation = invitation.TokenGeneration;

        var act = () => ResendHandler().Handle(
            new ResendResourceInvitationCommand(BoardKind, BoardId, invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        invitation.TokenGeneration.Should().Be(generation);
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task Resend_ForADifferentResource_IsNotFound()
    {
        var invitation = ResourceInvitation();
        SetupInvitations(invitation);

        var act = () => ResendHandler().Handle(
            new ResendResourceInvitationCommand(BoardKind, Guid.CreateVersion7(), invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task Resend_OfAWorkspaceInvitation_IsNotFound()
    {
        var workspaceInvitation = WorkspaceInvitation.Create(
            TestAccountId, TestWorkspaceId, "m@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(Hash), 1, TestUserId, TestNow.AddDays(-1));
        SetupInvitations(workspaceInvitation);

        var act = () => ResendHandler().Handle(
            new ResendResourceInvitationCommand(BoardKind, BoardId, workspaceInvitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Resend_OfARevokedInvitation_IsRejectedByTheDomain()
    {
        var invitation = ResourceInvitation();
        invitation.Revoke(TestUserId, TestNow);
        SetupInvitations(invitation);

        var act = () => ResendHandler().Handle(
            new ResendResourceInvitationCommand(BoardKind, BoardId, invitation.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Invitation_CannotResendNonPendingExpired);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Revoked);
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task Resend_WhenTheResourceIsGone_IsNotFound()
    {
        var invitation = ResourceInvitation();
        SetupInvitations(invitation);
        _facts.Setup(f => f.ResolveTitleAsync(BoardKind, BoardId, TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var act = () => ResendHandler().Handle(
            new ResendResourceInvitationCommand(BoardKind, BoardId, invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _published.Should().BeEmpty();
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Revoke_ByTheIssuer_RevokesAndTheTokenCanNoLongerBeAccepted()
    {
        var invitation = ResourceInvitation();
        SetupInvitations(invitation);

        var result = await RevokeHandler().Handle(
            new RevokeResourceInvitationCommand(BoardKind, BoardId, invitation.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Revoked);
        FluentActions.Invoking(() => invitation.Accept(Guid.CreateVersion7(), TestNow))
            .Should().Throw<DomainBusinessRuleException>()
            .Where(e => e.RuleCode == WorkspaceRuleCodes.Workspaces_Invitation_NotPending);
    }

    [Fact]
    public async Task Revoke_OfAnotherMembersInvitation_IsNotFoundAndLeavesItPending()
    {
        var invitation = ResourceInvitation(invitedBy: Guid.CreateVersion7());
        SetupInvitations(invitation);

        var act = () => RevokeHandler().Handle(
            new RevokeResourceInvitationCommand(BoardKind, BoardId, invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task Revoke_ForADifferentResource_IsNotFound()
    {
        var invitation = ResourceInvitation();
        SetupInvitations(invitation);

        var act = () => RevokeHandler().Handle(
            new RevokeResourceInvitationCommand(BoardKind, Guid.CreateVersion7(), invitation.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    // ── Workspace Owner/Admin resend keeps the resource email ────────────────

    [Fact]
    public async Task WorkspaceResend_OfAResourceInvitation_KeepsTheResourceFields()
    {
        var invitation = ResourceInvitation(invitedBy: TestUserId);
        SetupInvitations(invitation);
        SetupMembers(CreateMember(WorkspaceRole.Admin));
        var handler = new ResendInvitationCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, _tokens.Object, _encryptor.Object,
            _events.Object, DateTimeProviderMock.Object, _actors.Object, _facts.Object);

        var result = await handler.Handle(new ResendInvitationCommand(TestWorkspaceId, invitation.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var published = _published.Single().Should().BeOfType<WorkspaceInvitationDeliveryRequestedIntegrationEventV1>().Subject;
        published.ResourceKind.Should().Be(BoardKind);
        published.ResourceName.Should().Be("Triển khai ERP Q4");
    }

    [Fact]
    public async Task WorkspaceResend_OfAWorkspaceInvitation_CarriesNoResourceFields()
    {
        var invitation = WorkspaceInvitation.Create(
            TestAccountId, TestWorkspaceId, "m@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(Hash), 1, TestUserId, TestNow.AddDays(-1));
        SetupInvitations(invitation);
        SetupMembers(CreateMember(WorkspaceRole.Admin));
        var handler = new ResendInvitationCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, _tokens.Object, _encryptor.Object,
            _events.Object, DateTimeProviderMock.Object, _actors.Object, _facts.Object);

        await handler.Handle(new ResendInvitationCommand(TestWorkspaceId, invitation.Id), CancellationToken.None);

        var published = _published.Single().Should().BeOfType<WorkspaceInvitationDeliveryRequestedIntegrationEventV1>().Subject;
        published.ResourceKind.Should().BeNull();
        published.ResourceName.Should().BeNull();
    }

    // ── Invite: resource event fields ────────────────────────────────────────

    [Fact]
    public async Task InviteGuestToResource_QueuesTheResourceNameAndInviter()
    {
        var handler = new InviteGuestToResourceCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, _tokens.Object,
            _encryptor.Object, _events.Object, _audit.Object, _actors.Object, _facts.Object);

        var result = await handler.Handle(
            new InviteGuestToResourceCommand(BoardKind, BoardId, "Guest@Example.com", "Editor"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var published = _published.Single().Should().BeOfType<WorkspaceInvitationDeliveryRequestedIntegrationEventV1>().Subject;
        published.ResourceKind.Should().Be(BoardKind);
        published.ResourceId.Should().Be(BoardId);
        published.ResourceName.Should().Be("Triển khai ERP Q4");
        published.InviterName.Should().Be("Mai Linh");
        published.RecipientEmail.Should().Be("guest@example.com");
    }

    // ── Admin is not invitable ───────────────────────────────────────────────

    [Fact]
    public async Task InviteMember_AsAdmin_IsRejectedWithAStableRuleCode_AndQueuesNothing()
    {
        var handler = new InviteMemberCommandHandler(
            DbContextMock.Object, _actors.Object, RequestContextMock.Object, DateTimeProviderMock.Object,
            _tokens.Object, _encryptor.Object, _events.Object,
            new InvitationInitialEntries(DbContextMock.Object, new Mock<Notrelix.Application.Features.Workspaces.Spaces.Services.ISpaceStore>().Object),
            _memberEmails.Object);

        var act = () => handler.Handle(
            new InviteMemberCommand(TestWorkspaceId, "a@example.com", WorkspaceRole.Admin), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin);
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task ChangeInvitationRole_ToAdmin_IsRejected()
    {
        var invitation = WorkspaceInvitation.Create(
            TestAccountId, TestWorkspaceId, "m@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(Hash), 1, TestUserId, TestNow.AddDays(-1));
        SetupInvitations(invitation);
        var handler = new Notrelix.Application.Features.Workspaces.Invitations.Commands.ChangeInvitationRole.ChangeInvitationRoleCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(
            new Notrelix.Application.Features.Workspaces.Invitations.Commands.ChangeInvitationRole.ChangeInvitationRoleCommand(
                TestWorkspaceId, invitation.Id, WorkspaceRole.Admin), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Invitation_CannotInviteAsAdmin);
        invitation.Role.Should().Be(WorkspaceRole.Member);
    }
}
