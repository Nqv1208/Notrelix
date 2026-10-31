using Notrelix.Application.Common.Data.Rls;
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

    // An unconfirmed address is answered with an empty list by the handler, so the pipeline must not refuse the
    // request first (a 403 would show the rail an error where it should show "nothing waiting").
    [Fact]
    public void TheQuery_DoesNotRequireAVerifiedEmail() =>
        new GetUserPendingInvitationsQuery().Should().NotBeAssignableTo<Notrelix.Application.Common.Requests.Security.IRequireVerifiedEmail>();

    private static string Digest(int n) => new string('a', 62) + n.ToString("x2");

    [Fact]
    public async Task TheList_HoldsTheNewestMaxPending_AndNoMore()
    {
        StoredUser();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        var all = Enumerable.Range(0, InviteeInvitations.MaxPending + 5)
            .Select(n => CreateInvitation(tokenHash: Digest(n), createdAt: TestNow.AddHours(-1 - n)))
            .ToArray();
        SetupInvitations(all);

        var result = await Read();

        result.Data!.Select(d => d.Id).Should().Equal(all.Take(InviteeInvitations.MaxPending).Select(i => i.Id));
    }

    [Fact]
    public async Task InvitationsToOneWorkspaceFromOnePerson_EnterTheScopeAndReadTheNamesOnce()
    {
        StoredUser();
        SetupWorkspaces(CreateWorkspace(TestWorkspaceId));
        SetupInvitations(Enumerable.Range(0, 3).Select(n => CreateInvitation(tokenHash: Digest(n), createdAt: TestNow.AddHours(-1 - n))).ToArray());
        RlsMock.Setup(r => r.IsSessionContextEnabled).Returns(true);
        RlsMock.Setup(r => r.EnterTokenScopeAsync(It.IsAny<TokenScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenScopeBinding(TestAccountId, TestWorkspaceId));
        _actors.Setup(a => a.FindAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorSnapshot(TestUserId, "Ada", null));

        var result = await Read();

        result.Data.Should().HaveCount(3).And.OnlyContain(d => d.InviterName == "Ada");
        RlsMock.Verify(r => r.EnterTokenScopeAsync(It.IsAny<TokenScopeRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _actors.Verify(a => a.FindAsync(TestUserId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
