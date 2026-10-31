using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetUserPendingInvitations;
using Notrelix.Application.Features.Workspaces.Invitations.Services;
using Notrelix.Application.Tests.Features.Workspaces.Invitations.Commands.AcceptInvitation;

namespace Notrelix.Application.Tests.Features.Workspaces.Invitations;

/// <summary>
/// The invitations addressed to the signed-in person: the address is the stored one while it is confirmed, never
/// the email claim of the token, and a person who cannot take part or has not confirmed the address gets none.
/// </summary>
public class GetUserPendingInvitationsQueryHandlerTests : AcceptInvitationHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actors = new();

    private GetUserPendingInvitationsQueryHandler CreateReader() => new(
        new InviteeInvitations(DbContextMock.Object, CreateTokenScope(), _actors.Object),
        IdentityUserFactsMock.Object,
        RequestContextMock.Object,
        DateTimeProviderMock.Object);

    private Task<Notrelix.Application.Common.Models.Result<List<UserPendingInvitationDto>>> Read() =>
        CreateReader().Handle(new GetUserPendingInvitationsQuery(), CancellationToken.None);

    private void StoredUser(bool emailConfirmed = true, bool canParticipate = true, string email = TestEmail) =>
        IdentityUserFactsMock
            .Setup(f => f.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserFact(emailConfirmed, canParticipate, email));

    [Fact]
    public async Task AConfirmedPerson_SeesTheInvitationsAddressedToTheirStoredEmail_EvenWhenTheClaimNamesAnotherAddress()
    {
        RequestContextMock.Setup(c => c.Email).Returns(OtherEmail);
        StoredUser();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation(), CreateInvitation(email: OtherEmail, tokenHash: OtherTokenHash));

        var result = await Read();

        result.Succeeded.Should().BeTrue();
        result.Data!.Should().ContainSingle().Which.Email.Should().Be(TestEmail);
    }

    [Fact]
    public async Task AnUnconfirmedAddress_GetsNone_EvenWhenTheClaimMatchesAnInvitation()
    {
        RequestContextMock.Setup(c => c.Email).Returns(TestEmail);
        StoredUser(emailConfirmed: false);
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation());

        var result = await Read();

        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task APersonWhoCannotTakePart_GetsNone()
    {
        RequestContextMock.Setup(c => c.Email).Returns(TestEmail);
        StoredUser(canParticipate: false);
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation());

        (await Read()).Data.Should().BeEmpty();
    }

    [Fact]
    public async Task AStoredEmailThatDiffersFromTheClaim_DoesNotOpenTheClaimsInvitations()
    {
        RequestContextMock.Setup(c => c.Email).Returns(TestEmail);
        StoredUser(email: "renamed@example.com");
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(CreateInvitation());

        (await Read()).Data.Should().BeEmpty();
    }

    [Fact]
    public async Task APersonWhoIsNotSignedIn_GetsNone_WithoutLookingAnyoneUp()
    {
        RequestContextMock.Setup(c => c.IsAuthenticated).Returns(false);

        (await Read()).Data.Should().BeEmpty();
        IdentityUserFactsMock.Verify(f => f.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void TheQuery_RequiresAVerifiedEmail() =>
        new GetUserPendingInvitationsQuery().Should().BeAssignableTo<Notrelix.Application.Common.Requests.Security.IRequireVerifiedEmail>();
}
