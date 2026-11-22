using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Application.Features.Identity.Public.Facts;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;

/// <summary>
/// Shared-transaction invitation acceptance behavior (TAC-WG-001/002):
/// each gate is denied before any protected effect; the already-member path
/// completes the invitation without a second account/workspace mutation or
/// grant sync; the new-member path orchestrates the full BOUND-TX-002 flow.
/// </summary>
public class AcceptInvitationCommandHandlerTests : AcceptInvitationHandlerTestBase
{
    private AcceptInvitationCommand Command(string token = ValidInviteToken)
        => new(token);

    [Fact]
    public async Task Handle_WhenUnauthenticated_ReturnsLoginRequiredFailure()
    {
        RequestContextMock.Setup(c => c.IsAuthenticated).Returns(false);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("You must be logged in to perform this action.");
        IdentityUserFactsMock.Verify(
            f => f.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserDoesNotExist_ReturnsFailure()
    {
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityUserFact?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Current user was not found.");
    }

    [Fact]
    public async Task Handle_WhenUserCannotParticipate_ReturnsInactiveAccountFailureWithoutTokenLookup()
    {
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact(canParticipate: false));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(
            "Your account must be active before accepting workspace invitations.");
        OneTimeTokenServiceMock.Verify(
            t => t.ParseAndHash(It.IsAny<string>(), It.IsAny<TokenPurpose>()), Times.Never);
    }

    // The emailed token proves control of the invited address, so for the token path an unconfirmed
    // sign-in email is confirmed by the acceptance itself (AcceptInvitationById, with no token, stays gated).
    private void UnconfirmedUser() =>
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact(emailConfirmed: false));

    private void VerifyEmailNeverConfirmed() =>
        InvitedRegistrationMock.Verify(
            r => r.ConfirmEmailAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndTheTokenIsForTheirAddress_ConfirmsTheEmailAndAccepts()
    {
        UnconfirmedUser();
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers();

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
        InvitedRegistrationMock.Verify(
            r => r.ConfirmEmailAsync(TestUserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyConfirmed_DoesNotConfirmAgain()
    {
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers();

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndTheTokenIsForAnotherAddress_IsRefusedAndConfirmsNothing()
    {
        UnconfirmedUser();
        SetupInvitations(CreateInvitation(email: OtherEmail));

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation belongs to a different email address.");
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndTheTokenIsUnparseable_IsRefusedAndConfirmsNothing()
    {
        UnconfirmedUser();
        OneTimeTokenServiceMock
            .Setup(t => t.ParseAndHash(It.IsAny<string>(), TokenPurpose.WorkspaceInvitation))
            .Throws(new InvalidOneTimeTokenException());
        SetupInvitations(CreateInvitation());

        var result = await CreateSut().Handle(Command("v1.not-a-real-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid or expired invitation token.");
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndNoInvitationMatchesTheToken_ThrowsNotFoundAndConfirmsNothing()
    {
        UnconfirmedUser();
        SetupInvitations(CreateInvitation());
        OneTimeTokenServiceMock
            .Setup(t => t.ParseAndHash(It.IsAny<string>(), TokenPurpose.WorkspaceInvitation))
            .Returns(new ParsedOneTimeToken(OtherTokenHash, HashVersion: 1));

        var act = () => CreateSut().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndTheInvitationExpired_IsRefusedAndConfirmsNothing()
    {
        UnconfirmedUser();
        SetupInvitations(CreateInvitation(createdAt: TestNow.AddDays(-8)));

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation has expired.");
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndTheInvitationWasRevoked_IsRefusedAndConfirmsNothing()
    {
        UnconfirmedUser();
        var invitation = CreateInvitation();
        invitation.Revoke(TestUserId, TestNow);
        SetupInvitations(invitation);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation is no longer valid.");
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndTheAccountCannotAdmit_IsRefusedAndConfirmsNothing()
    {
        UnconfirmedUser();
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        AccountMembershipFactsMock
            .Setup(f => f.GetAdmissionAsync(TestAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountMembershipAdmissionFact(CanAdmitMember: false));

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndTheWorkspaceIsInactive_IsRefusedAndConfirmsNothing()
    {
        UnconfirmedUser();
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId, isArchived: true));

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenEmailUnconfirmed_AndTheUserCannotParticipate_IsRefusedAndConfirmsNothing()
    {
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact(emailConfirmed: false, canParticipate: false));

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        VerifyEmailNeverConfirmed();
    }

    [Fact]
    public async Task Handle_WhenTokenInvalid_ReturnsInvalidTokenFailureBeforeInvitationLookup()
    {
        OneTimeTokenServiceMock
            .Setup(t => t.ParseAndHash(It.IsAny<string>(), TokenPurpose.WorkspaceInvitation))
            .Throws(new InvalidOneTimeTokenException());
        SetupInvitations(CreateInvitation());

        var sut = CreateSut();

        var result = await sut.Handle(Command("v1.not-a-real-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid or expired invitation token.");
    }

    [Fact]
    public async Task Handle_WhenInvitationHashDoesNotMatch_ThrowsNotFoundException()
    {
        var invitation = CreateInvitation(); // stored hash: ValidTokenHash
        SetupInvitations(invitation);
        OneTimeTokenServiceMock
            .Setup(t => t.ParseAndHash(It.IsAny<string>(), TokenPurpose.WorkspaceInvitation))
            .Returns(new ParsedOneTimeToken(OtherTokenHash, HashVersion: 1));

        var sut = CreateSut();

        var act = () => sut.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenInvitationExpired_ReturnsExpiredFailureWithoutWorkspaceMutation()
    {
        SetupInvitations(CreateInvitation(createdAt: TestNow.AddDays(-8)));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation has expired.");
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvitationNotPending_ReturnsNotValidFailureWithoutMutation()
    {
        var invitation = CreateInvitation();
        invitation.Revoke(TestUserId, TestNow);
        SetupInvitations(invitation);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation is no longer valid.");
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailsDoNotMatch_ReturnsEmailMismatchFailureWithoutMutation()
    {
        SetupInvitations(CreateInvitation(email: OtherEmail));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(
            "This invitation belongs to a different email address.");
        AccountMembershipActionsMock.Verify(
            a => a.EnsureAccountMembershipAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenWorkspaceDoesNotExist_ThrowsNotFoundException()
    {
        SetupInvitations(CreateInvitation());
        SetupWorkspaces();

        var sut = CreateSut();

        var act = () => sut.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceInactive_ReturnsInactiveWorkspaceFailure()
    {
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId, isArchived: true));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(
            "Cannot accept invitation for an inactive workspace.");
    }

    [Fact]
    public async Task Handle_WhenAccountDoesNotExist_ReturnsAccountNotFoundFailure()
    {
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        AccountMembershipFactsMock
            .Setup(f => f.GetAdmissionAsync(TestAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountMembershipAdmissionFact?)null);

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Account was not found.");
    }

    [Fact]
    public async Task Handle_WhenAccountCannotAdmit_ReturnsInactiveAccountFailureWithoutMutation()
    {
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        AccountMembershipFactsMock
            .Setup(f => f.GetAdmissionAsync(TestAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountMembershipAdmissionFact(CanAdmitMember: false));

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(
            "Cannot accept invitation for an inactive account.");
        AccountMembershipActionsMock.Verify(
            a => a.EnsureAccountMembershipAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAlreadyWorkspaceMember_AcceptsInvitationWithoutSecondMutation()
    {
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember());

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.WorkspaceId.Should().Be(TestWorkspaceId);
        result.Data.WorkspaceSlug.Should().Be("test-workspace");

        DbContextMock.Object.WorkspaceInvitations.Single().Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        AccountMembershipActionsMock.Verify(
            a => a.EnsureAccountMembershipAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "an already-member acceptance must not trigger a second Account membership mutation");
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "an already-member acceptance must not sync a second workspace grant");
    }

    [Fact]
    public async Task Handle_WhenAGuestInvitationIsAcceptedWhileGuestInvitesAreOff_IsRefusedWithTheStableCode_AndAdmitsNobody()
    {
        SetupInvitations(CreateInvitation(role: WorkspaceRole.Guest));
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers();

        var act = () => CreateSut().Handle(Command(), CancellationToken.None);

        (await act.Should().ThrowAsync<ForbiddenException>()).Which.Code.Should().Be("workspace.guest_invites_off");
        DbContextMock.Object.WorkspaceInvitations.Single().Status.Should().Be(WorkspaceInvitationStatus.Pending);
        DbContextMock.Object.WorkspaceMembers.Should().BeEmpty();
        SeatGateMock.Verify(
            g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAGuestInvitationIsAcceptedWhileGuestInvitesAreOn_IsAdmitted()
    {
        SetupInvitations(CreateInvitation(role: WorkspaceRole.Guest));
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId, guestInvites: true));
        SetupMembers();

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join(";", result.Errors));
    }

    [Theory]
    [InlineData(WorkspaceRole.Member, "member limit")]
    [InlineData(WorkspaceRole.Guest, "guest limit")]
    public async Task Handle_WhenThePlanHasNoFreeSeat_FailsWithThePlanLimitMessage_AndAdmitsNobody(WorkspaceRole role, string limit)
    {
        SetupInvitations(CreateInvitation(role: role));
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId, guestInvites: true));
        SetupMembers();
        SeatGateMock
            .Setup(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain(limit);
        DbContextMock.Object.WorkspaceInvitations.Single().Status.Should().Be(WorkspaceInvitationStatus.Pending, "a refused acceptance leaves the invitation open");
        DbContextMock.Object.WorkspaceMembers.Should().BeEmpty();
        AccountMembershipActionsMock.Verify(
            a => a.EnsureAccountMembershipAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenARemovedMemberReturnsAndNoSeatIsFree_ThrowsSeatUnavailable_AndStaysRemoved()
    {
        var removed = CreateMember(userId: TestUserId);
        removed.Remove(activeOwnerCount: 2, Guid.NewGuid(), TestNow.AddHours(1));
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(removed);
        SeatGateMock
            .Setup(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var accept = () => CreateSut().Handle(Command(), CancellationToken.None);

        (await accept.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Member_SeatUnavailable);
        removed.Status.Should().Be(WorkspaceMemberStatus.Removed);
        DbContextMock.Object.WorkspaceInvitations.Single().Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_WhenTheMemberIsSuspended_IsRefusedAndStaysSuspended_EvenWithAFreeSeat()
    {
        var suspended = CreateMember(userId: TestUserId);
        suspended.Suspend(Guid.NewGuid(), TestNow.AddHours(1), activeOwnerCount: 2);
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(suspended);

        var result = await CreateSut().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Your membership in this workspace is not active.");
        suspended.Status.Should().Be(WorkspaceMemberStatus.Suspended, "an invitation never overrides an admin's suspension");
        DbContextMock.Object.WorkspaceInvitations.Single().Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task Handle_WhenAnswerIsRepeated_TheSecondOneIsRefusedAndAddsNoSecondMembership()
    {
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers();

        var first = await CreateSut().Handle(Command(), CancellationToken.None);
        var again = await CreateSut().Handle(Command(), CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        again.Succeeded.Should().BeFalse("the invitation is no longer pending");
        DbContextMock.Object.WorkspaceMembers.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WhenNewMember_AcceptsAndOrchestratesFullInviteeProvisioning()
    {
        SetupInvitations(CreateInvitation());
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers();

        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.WorkspaceId.Should().Be(TestWorkspaceId);
        result.Data.WorkspaceSlug.Should().Be("test-workspace");

        DbContextMock.Object.WorkspaceInvitations.Single().Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        DbContextMock.Object.WorkspaceMembers.Should().ContainSingle();
        DbContextMock.Object.WorkspaceMembers.Single().UserId.Should().Be(TestUserId);
        DbContextMock.Object.WorkspaceMembers.Single().WorkspaceId.Should().Be(TestWorkspaceId);

        AccountMembershipActionsMock.Verify(
            a => a.EnsureAccountMembershipAsync(
                TestAccountId, TestUserId, It.IsAny<Guid>(), TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                TestAccountId, TestWorkspaceId, TestUserId, WorkspaceRole.Member, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}