using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Public.InvitedRegistration;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationWithRegistration;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

/// <summary>
/// Accepting an invitation by creating the account in the same step: the invited address is fixed by
/// the invitation, every rejection leaves nothing behind, and a second press is the same success.
/// </summary>
public class AcceptInvitationWithRegistrationTests : AcceptInvitationHandlerTestBase
{
    private readonly Mock<IInvitedUserRegistrationActions> _registration = new();
    private readonly Guid _newUserId = Guid.CreateVersion7();

    private AcceptInvitationWithRegistrationCommandHandler CreateHandler() => new(
        DbContextMock.Object,
        CreateTokenScope(),
        OneTimeTokenServiceMock.Object,
        DateTimeProviderMock.Object,
        _registration.Object,
        new InvitationAcceptanceService(
            DbContextMock.Object,
            AccountMembershipActionsMock.Object,
            AccountMembershipFactsMock.Object,
            GrantProjectionMock.Object,
            new InvitationInitialEntries(
                DbContextMock.Object, new Mock<Notrelix.Application.Features.Workspaces.Spaces.Services.ISpaceStore>().Object),
            new Mock<Notrelix.Application.Features.Governance.Public.Access.IGuestResourceAccessActions>().Object,
            new Mock<Notrelix.Application.Common.Auditing.IAuditService>().Object,
            SeatGateMock.Object));

    private AcceptInvitationWithRegistrationCommand Command() => new(ValidInviteToken, "Thu Ha", "a-long-enough-password");

    private void RegistrationSucceeds()
    {
        _registration
            .Setup(r => r.RegisterAsync(TestEmail, "Thu Ha", "a-long-enough-password", TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvitedUserRegistration(_newUserId, TestEmail, "Thu Ha", "a", "r", TestNow.UtcDateTime.AddHours(1)));
        AccountMembershipActionsMock
            .Setup(a => a.EnsureAccountMembershipAsync(
                TestAccountId, _newUserId, It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task NewPerson_IsRegisteredWithTheInvitedAddress_JoinsOnce_AndGetsASession()
    {
        RegistrationSucceeds();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        var invitation = CreateInvitation();
        SetupInvitations(invitation);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AlreadyAccepted.Should().BeFalse();
        result.Data.Session!.AccessToken.Should().Be("a");
        result.Data.Acceptance.WorkspaceId.Should().Be(TestWorkspaceId);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
        DbContextMock.Verify(c => c.WorkspaceMembers.Add(It.Is<WorkspaceMember>(m => m.UserId == _newUserId && m.Role == WorkspaceRole.Member)), Times.Once);
    }

    [Fact]
    public async Task AdoptsTheInvitationsWorkspaceScope_FirstWithoutAUser_ThenWithTheNewUser()
    {
        RegistrationSucceeds();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation());

        await CreateHandler().Handle(Command(), CancellationToken.None);

        TenantMock.Verify(t => t.SetWorkspace(TestAccountId, TestWorkspaceId, null), Times.Once);
        TenantMock.Verify(t => t.SetWorkspace(TestAccountId, TestWorkspaceId, _newUserId), Times.Once);
        RlsMock.Verify(r => r.ApplyAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SecondPress_AnswersTheSameSuccess_WithoutRegisteringOrASession()
    {
        var invitation = CreateInvitation();
        invitation.Accept(_newUserId, TestNow);
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(invitation);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AlreadyAccepted.Should().BeTrue();
        result.Data.Session.Should().BeNull();
        _registration.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddressWithAnAccount_IsAConflict_AndNothingJoins()
    {
        _registration
            .Setup(r => r.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvitedUserRegistration?)null);
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        var invitation = CreateInvitation();
        SetupInvitations(invitation);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Single().Type.Should().Be(ApplicationErrorType.Conflict);
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
        DbContextMock.Verify(c => c.WorkspaceMembers.Add(It.IsAny<WorkspaceMember>()), Times.Never);
    }

    [Fact]
    public async Task ExpiredInvitation_CreatesNoAccount()
    {
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation(createdAt: TestNow.AddDays(-30)));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation has expired.");
        _registration.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeclinedInvitation_CreatesNoAccount()
    {
        var invitation = CreateInvitation();
        invitation.Decline(null, TestNow);
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(invitation);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _registration.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AcceptanceRuleRejectingAfterRegistration_ThrowsSoTheIdentityRollsBackToo()
    {
        RegistrationSucceeds();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId, isArchived: true));
        SetupInvitations(CreateInvitation());

        Func<Task> act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.BusinessRuleException>()
            .WithMessage("Cannot accept invitation for an inactive workspace.");
    }

    [Fact]
    public async Task UnknownToken_IsNotFound()
    {
        SetupInvitations();

        Func<Task> act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Theory]
    [InlineData("", "a-long-enough-password", "Name")]
    [InlineData("Thu Ha", "short", "Password")]
    public void Validator_RejectsAMissingNameOrAShortPassword(string name, string password, string field) =>
        new AcceptInvitationWithRegistrationCommandValidator()
            .Validate(new AcceptInvitationWithRegistrationCommand(ValidInviteToken, name, password))
            .Errors.Should().ContainSingle(e => e.PropertyName == field);
}
