using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetInvitationByToken;
using Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations;

/// <summary>
/// The join page read (accepted 2026-10-24): the holder of an invitation token sees the inviter's display
/// name, never the inviter's email or any other identity fact.
/// </summary>
public class GetInvitationByTokenQueryHandlerTests : AcceptInvitationHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actors = new();

    private GetInvitationByTokenQueryHandler CreateReader() => new(
        DbContextMock.Object, _actors.Object, DateTimeProviderMock.Object, OneTimeTokenServiceMock.Object);

    [Fact]
    public async Task TokenHolder_SeesTheInviterDisplayName_AndTheWorkspaceName()
    {
        var inviter = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation(invitedBy: inviter));
        _actors.Setup(a => a.FindAsync(inviter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorSnapshot(inviter, "Mai Linh", null));

        var result = await CreateReader().Handle(new GetInvitationByTokenQuery(ValidInviteToken), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.InviterName.Should().Be("Mai Linh");
        result.Data.WorkspaceName.Should().Be("Test Workspace");
        result.Data.Email.Should().Be(TestEmail, "the invited address is the token holder's own");
    }

    [Fact]
    public void TheReadModel_CarriesNoInviterEmailOrOtherInviterFact() =>
        typeof(WorkspaceInvitationDto).GetProperties().Select(p => p.Name).Should().BeEquivalentTo(
            "Id", "WorkspaceName", "InviterName", "Email", "Role", "IsExpired", "IsAccepted", "ResourceKind", "ResourceLevel", "InitialEntryCount");

    [Fact]
    public async Task InviterWithoutAName_FallsBackToAGenericLabel_NeverToAnEmail()
    {
        var inviter = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation(invitedBy: inviter));
        _actors.Setup(a => a.FindAsync(inviter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorSnapshot(inviter, " ", null));

        var result = await CreateReader().Handle(new GetInvitationByTokenQuery(ValidInviteToken), CancellationToken.None);

        result.Data!.InviterName.Should().NotBeNullOrWhiteSpace().And.NotContain("@");
    }

    [Fact]
    public async Task UnknownToken_IsNotFound()
    {
        SetupInvitations();

        Func<Task> act = () => CreateReader().Handle(new GetInvitationByTokenQuery(ValidInviteToken), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
