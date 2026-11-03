using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Identity.Public.Facts;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationById;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

/// <summary>
/// Accepting from the person's own list: the invitation is opened for the signed-in person by id, then the rules of
/// accepting by link apply. An id that is unknown, another address's, or no longer pending is the same NotFound.
/// </summary>
public class AcceptInvitationByIdTests : AcceptInvitationHandlerTestBase
{
    private Task<Notrelix.Application.Common.Models.Result<AcceptInvitationResultDto>> Accept(Guid id) =>
        new AcceptInvitationByIdCommandHandler(
            new InviteeInvitations(DbContextMock.Object, CreateTokenScope(), new Mock<IActorLookupService>().Object),
            new InvitationAcceptanceService(
                DbContextMock.Object,
                AccountMembershipActionsMock.Object,
                AccountMembershipFactsMock.Object,
                GrantProjectionMock.Object,
                new InvitationInitialEntries(DbContextMock.Object, new Mock<ISpaceStore>().Object),
                new Mock<IGuestResourceAccessActions>().Object,
                new Mock<IAuditService>().Object,
                SeatGateMock.Object),
            IdentityUserFactsMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object).Handle(new AcceptInvitationByIdCommand(id), CancellationToken.None);

    private void ActiveWorkspace() => SetupWorkspaces(CreateWorkspace(TestWorkspaceId));

    [Fact]
    public async Task APersonWithAConfirmedAddress_AcceptsTheirInvitation_AndBecomesAMember()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        ActiveWorkspace();
        SetupMembers();

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeTrue();
        result.Data!.WorkspaceId.Should().Be(TestWorkspaceId);
        result.Data.WorkspaceSlug.Should().Be("test-workspace");
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        DbContextMock.Object.WorkspaceMembers.Should().ContainSingle().Which.UserId.Should().Be(TestUserId);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                TestAccountId, TestWorkspaceId, TestUserId, WorkspaceRole.Member, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task APersonWhoIsNotSignedIn_IsRefused_WithoutLookingAnyoneUp()
    {
        RequestContextMock.Setup(c => c.IsAuthenticated).Returns(false);
        var invitation = CreateInvitation();
        SetupInvitations(invitation);

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("You must be logged in to perform this action.");
        IdentityUserFactsMock.Verify(f => f.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task AnUnknownCurrentUser_IsRefused_AndTheInvitationStaysPending()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserFact?)null);

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Current user was not found.");
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task APersonWhoCannotTakePart_IsRefused_AndTheInvitationStaysPending()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact(canParticipate: false));

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Your account must be active before accepting workspace invitations.");
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task AnUnconfirmedAddress_IsRefused_AndTheInvitationStaysPending()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact(emailConfirmed: false));

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Email must be confirmed before accepting workspace invitations.");
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task AnInvitationForAnotherAddress_AndAnUnknownId_GetTheSameNotFound_AndNothingChanges()
    {
        var other = CreateInvitation(email: OtherEmail);
        SetupInvitations(other);
        ActiveWorkspace();
        SetupMembers();

        var foreign = () => Accept(other.Id);
        var unknown = () => Accept(Guid.NewGuid());

        var found = (await foreign.Should().ThrowAsync<NotFoundException>()).Which;
        var made = (await unknown.Should().ThrowAsync<NotFoundException>()).Which;
        static string WithoutId(string message) => System.Text.RegularExpressions.Regex.Replace(message, "[0-9a-fA-F-]{32,36}", "<id>");
        WithoutId(found.Message).Should().Be(WithoutId(made.Message));
        other.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        DbContextMock.Object.WorkspaceMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task AnInvitationThatIsNoLongerPending_GetsNotFound_AndAddsNoSecondMembership()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        ActiveWorkspace();
        SetupMembers();

        (await Accept(invitation.Id)).Succeeded.Should().BeTrue();
        var again = () => Accept(invitation.Id);

        await again.Should().ThrowAsync<NotFoundException>();
        DbContextMock.Object.WorkspaceMembers.Should().ContainSingle();
    }

    [Fact]
    public async Task AnExpiredInvitation_IsRefusedWithTheExpiredMessage_AndAddsNoMember()
    {
        var invitation = CreateInvitation(createdAt: TestNow.AddDays(-10), expiry: TimeSpan.FromDays(7));
        SetupInvitations(invitation);
        ActiveWorkspace();
        SetupMembers();

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation has expired.");
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        DbContextMock.Object.WorkspaceMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task AnInvitationToAnInactiveWorkspace_IsRefused_AndAddsNoMember()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId, isArchived: true));
        SetupMembers();

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Cannot accept invitation for an inactive workspace.");
        DbContextMock.Object.WorkspaceMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task AnAccountThatCannotAdmit_RefusesTheInvitation()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        ActiveWorkspace();
        SetupMembers();
        AccountMembershipFactsMock
            .Setup(f => f.GetAdmissionAsync(TestAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountMembershipAdmissionFact(CanAdmitMember: false));

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Cannot accept invitation for an inactive account.");
        DbContextMock.Object.WorkspaceMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenNoSeatIsFree_TheInvitationIsRefused_AndAddsNoMember()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        ActiveWorkspace();
        SetupMembers();
        SeatGateMock
            .Setup(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainMatch("The member limit*");
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        DbContextMock.Object.WorkspaceMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task ASuspendedMember_IsRefused_AndStaysSuspended()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        ActiveWorkspace();
        var suspended = CreateMember();
        suspended.Suspend(TestUserId, TestNow, activeOwnerCount: 2);
        SetupMembers(suspended);

        var result = await Accept(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Your membership in this workspace is not active.");
        suspended.Status.Should().Be(WorkspaceMemberStatus.Suspended);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }
}
