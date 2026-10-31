using Notrelix.Application.Common.Tenancy;
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
}
