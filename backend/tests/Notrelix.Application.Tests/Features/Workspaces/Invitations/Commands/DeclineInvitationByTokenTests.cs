using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.DeclineInvitationByToken;
using Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;
using Notrelix.Domain.Common;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Invitations.Events;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands;

/// <summary>Declining by link: the token is the authority, the person may have no account.</summary>
public class DeclineInvitationByTokenTests : AcceptInvitationHandlerTestBase
{
    private DeclineInvitationByTokenCommandHandler CreateHandler() =>
        new(CreateTokenScope(), OneTimeTokenServiceMock.Object, DateTimeProviderMock.Object);

    private Task<Result> Decline() =>
        CreateHandler().Handle(new DeclineInvitationByTokenCommand(ValidInviteToken), CancellationToken.None);

    [Fact]
    public async Task PendingInvitation_IsDeclined_WithNoActor()
    {
        var invitation = CreateInvitation();
        SetupInvitations(invitation);
        ((IHasDomainEvents)invitation).ClearDomainEvents();

        var result = await Decline();

        result.Succeeded.Should().BeTrue();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Declined);
        invitation.DomainEvents.OfType<WorkspaceInvitationDeclinedDomainEvent>().Single().DeclinedBy.Should().BeNull();
    }

    [Fact]
    public async Task UnknownToken_IsNotFound_AndAdoptsNoScope()
    {
        SetupInvitations();

        var act = () => Decline();

        await act.Should().ThrowAsync<NotFoundException>();
        TenantMock.Verify(t => t.SetWorkspace(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task KnownToken_AdoptsOnlyTheInvitationsWorkspaceScope_WithoutAUser()
    {
        SetupInvitations(CreateInvitation());

        await Decline();

        TenantMock.Verify(t => t.SetWorkspace(TestAccountId, TestWorkspaceId, null), Times.Once);
        RlsMock.Verify(r => r.ApplyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SecondPress_AnswersTheSameSuccess_WithoutASecondEvent()
    {
        var invitation = CreateInvitation();
        invitation.Decline(null, TestNow);
        ((IHasDomainEvents)invitation).ClearDomainEvents();
        SetupInvitations(invitation);

        var result = await Decline();

        result.Succeeded.Should().BeTrue();
        invitation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptedInvitation_CannotBeDeclined()
    {
        var invitation = CreateInvitation();
        invitation.Accept(TestUserId, TestNow);
        SetupInvitations(invitation);

        var result = await Decline();

        result.Succeeded.Should().BeFalse();
        invitation.Status.Should().Be(WorkspaceInvitationStatus.Accepted);
    }

    [Fact]
    public async Task ExpiredInvitation_CannotBeDeclined()
    {
        SetupInvitations(CreateInvitation(createdAt: TestNow.AddDays(-30)));

        var result = await Decline();

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("This invitation has expired.");
    }

    [Fact]
    public async Task UnknownToken_IsNotFound()
    {
        SetupInvitations();

        Func<Task> act = Decline;

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
