using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetInvitationByToken;
using Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations;

/// <summary>
/// The public read of an invitation link: the token holder sees the inviter's display name, the
/// Workspace, and for a guest invitation the resource title and level. The invited email and whether
/// it has an account are shown only while the link can still be used; nothing else about the inviter
/// or the invitee leaves.
/// </summary>
public class GetInvitationByTokenQueryHandlerTests : AcceptInvitationHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IGuestResourceInvitationFacts> _resources = new();

    private GetInvitationByTokenQueryHandler CreateReader() => new(
        DbContextMock.Object, CreateTokenScope(), _actors.Object, DateTimeProviderMock.Object, OneTimeTokenServiceMock.Object,
        _resources.Object, IdentityUserFactsMock.Object);

    private void Inviter(Guid id, string? name) =>
        _actors.Setup(a => a.FindAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorSnapshot(id, name ?? "", null));

    private Task<Result<InvitationPreviewDto>> Read() =>
        CreateReader().Handle(new GetInvitationByTokenQuery(ValidInviteToken), CancellationToken.None);

    [Fact]
    public async Task TokenHolder_SeesTheInviterDisplayName_TheWorkspaceName_AndTheEmailWhileValid()
    {
        var inviter = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation(invitedBy: inviter));
        Inviter(inviter, "Mai Linh");
        IdentityUserFactsMock.Setup(f => f.ExistsByEmailAsync(TestEmail, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Read();

        result.Succeeded.Should().BeTrue();
        result.Data!.State.Should().Be("valid");
        result.Data.InviterName.Should().Be("Mai Linh");
        result.Data.WorkspaceName.Should().Be("Test Workspace");
        result.Data.Email.Should().Be(TestEmail, "the invited address is the token holder's own");
        result.Data.HasAccount.Should().BeTrue();
        result.Data.ExpiresOn.Should().Be(DateOnly.FromDateTime(TestNow.AddDays(6).UtcDateTime));
    }

    [Fact]
    public void TheReadModel_CarriesNoInviterEmailOrOtherInviterFact() =>
        typeof(InvitationPreviewDto).GetProperties().Select(p => p.Name).Should().BeEquivalentTo(
            "State", "InviterName", "WorkspaceName", "Role", "ResourceKind", "ResourceTitle", "ResourceLevel",
            "ExpiresOn", "Email", "HasAccount");

    [Fact]
    public async Task ExpiredLink_ShowsNoInviteeDetails_AndNeverAsksWhetherTheAddressHasAnAccount()
    {
        var inviter = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation(invitedBy: inviter, createdAt: TestNow.AddDays(-30), expiry: TimeSpan.FromDays(7)));
        Inviter(inviter, "Mai Linh");

        var result = await Read();

        result.Data!.State.Should().Be("expired");
        result.Data.Email.Should().BeNull();
        result.Data.HasAccount.Should().BeFalse();
        IdentityUserFactsMock.Verify(f => f.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptedLink_IsUsed_AndWithholdsTheEmail()
    {
        var inviter = Guid.CreateVersion7();
        var invitation = CreateInvitation(invitedBy: inviter);
        invitation.Accept(TestUserId, TestNow);
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(invitation);
        Inviter(inviter, "Mai Linh");

        var result = await Read();

        result.Data!.State.Should().Be("used");
        result.Data.Email.Should().BeNull();
    }

    [Fact]
    public async Task DeclinedLink_IsUsed()
    {
        var invitation = CreateInvitation();
        invitation.Decline(null, TestNow);
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(invitation);
        Inviter(TestUserId, "Mai Linh");

        var result = await Read();

        result.Data!.State.Should().Be("used");
    }

    [Fact]
    public async Task GuestInvitation_NamesTheResourceTitleAndLevel()
    {
        var inviter = Guid.CreateVersion7();
        var boardId = Guid.CreateVersion7();
        var invitation = WorkspaceInvitation.CreateForResource(
            TestAccountId, TestWorkspaceId, TestEmail,
            new WorkspaceInvitationResourceGrant(WorkspaceInvitationResourceGrant.BoardKind, boardId, "Commenter"),
            InvitationTokenHash.Create(ValidTokenHash), 1, inviter, TestNow.AddDays(-1), TimeSpan.FromDays(7));
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(invitation);
        Inviter(inviter, "Mai Linh");
        _resources.Setup(r => r.ResolveTitleAsync(WorkspaceInvitationResourceGrant.BoardKind, boardId, TestWorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ERP rollout");

        var result = await Read();

        result.Data!.Role.Should().Be("Guest");
        result.Data.ResourceTitle.Should().Be("ERP rollout");
        result.Data.ResourceLevel.Should().Be("Commenter");
    }

    [Fact]
    public async Task InviterWithoutAName_FallsBackToAGenericLabel_NeverToAnEmail()
    {
        var inviter = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation(invitedBy: inviter));
        Inviter(inviter, " ");

        var result = await Read();

        result.Data!.InviterName.Should().NotBeNullOrWhiteSpace().And.NotContain("@");
    }

    [Fact]
    public async Task UnknownToken_IsNotFound()
    {
        SetupInvitations();

        Func<Task> act = Read;

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MalformedToken_AnswersLikeAnUnknownOne()
    {
        OneTimeTokenServiceMock
            .Setup(t => t.ParseAndHash("garbage", TokenPurpose.WorkspaceInvitation))
            .Throws(new Notrelix.Application.Common.Tokens.InvalidOneTimeTokenException());

        Func<Task> act = () => CreateReader().Handle(new GetInvitationByTokenQuery("garbage"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
