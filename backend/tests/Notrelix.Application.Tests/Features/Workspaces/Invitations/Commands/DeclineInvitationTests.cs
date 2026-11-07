using Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitation;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Domain.Workspaces.Invitations;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

/// <summary>Declining from the person's own list: only someone who can take part, with a confirmed address, may.</summary>
public class DeclineInvitationTests : AcceptInvitationHandlerTestBase
{
    private Task<Notrelix.Application.Common.Models.Result> Decline(Guid id) =>
        new DeclineInvitationCommandHandler(
            new InviteeInvitations(DbContextMock.Object, CreateTokenScope(), new Mock<IActorLookupService>().Object),
            IdentityUserFactsMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object).Handle(new DeclineInvitationCommand(id), CancellationToken.None);

    [Fact]
    public async Task APersonWithAConfirmedAddress_DeclinesTheirInvitation()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);

        var result = await Decline(invitation.Id);

        result.Succeeded.Should().BeTrue();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Declined);
    }

    [Fact]
    public async Task APersonWhoCannotTakePart_GetsNotFound_AndTheInvitationStaysPending()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact(canParticipate: false));

        var act = () => Decline(invitation.Id);

        await act.Should().ThrowAsync<NotFoundException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task AnUnconfirmedAddress_GetsNotFound_AndTheInvitationStaysPending()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact(emailConfirmed: false));

        var act = () => Decline(invitation.Id);

        await act.Should().ThrowAsync<NotFoundException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }

    [Fact]
    public async Task AnInvitationForAnotherAddress_AndAnUnknownId_GetTheSameNotFound()
    {
        var other = CreateInvitation(email: OtherEmail);
        SetupInvitations(other);

        var foreign = () => Decline(other.Id);
        var unknown = () => Decline(Guid.NewGuid());

        var found = (await foreign.Should().ThrowAsync<NotFoundException>()).Which;
        var made = (await unknown.Should().ThrowAsync<NotFoundException>()).Which;
        static string WithoutId(string message) => System.Text.RegularExpressions.Regex.Replace(message, "[0-9a-fA-F-]{32,36}", "<id>");
        WithoutId(found.Message).Should().Be(WithoutId(made.Message), "another address answers like a made-up id");
        other.Status.Should().Be(WorkspaceInvitationStatus.Pending, "someone else's invitation is untouched");
    }

    [Fact]
    public async Task ARepeatedDecline_GetsNotFound_AndChangesNothing()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);

        (await Decline(invitation.Id)).Succeeded.Should().BeTrue();
        var version = invitation.Version;
        var again = () => Decline(invitation.Id);

        await again.Should().ThrowAsync<NotFoundException>();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Declined);
        invitation.Version.Should().Be(version, "a repeat bumps no version");
    }

    [Fact]
    public async Task AnInvitationThatRanOut_IsRefusedWithTheExpiredMessage_AndStaysPending()
    {
        var invitation = CreateInvitation(createdAt: TestNow.AddDays(-10));
        SetupInvitations(invitation);

        var result = await Decline(invitation.Id);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation has expired.");
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Pending);
    }
}
