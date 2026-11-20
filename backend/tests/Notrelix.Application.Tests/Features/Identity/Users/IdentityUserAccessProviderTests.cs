using Notrelix.Application.Features.Identity.Public.Access;
using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Users;

/// <summary>A disabled person loses every session, API token and issued access token at once.</summary>
public class IdentityUserAccessProviderTests : IdentityHandlerTestBase
{
    private static readonly Guid Admin = Guid.CreateVersion7();
    private static readonly Guid AccountId = Guid.CreateVersion7();

    private IdentityUserAccessProvider Sut() =>
        new(IdentityContextMock.Object, JwtBlacklistMock.Object, DateTimeProviderMock.Object);

    private ApiToken Token(Guid userId, Guid workspaceId, string hash) =>
        ApiToken.Create(AccountId, workspaceId, userId, "Token", hash, null, userId, TestNow, TestNow.AddDays(30));

    [Theory]
    [InlineData(UserAccessEnd.Deactivated, UserStatus.Inactive)]
    [InlineData(UserAccessEnd.Suspended, UserStatus.Suspended)]
    public async Task EndAccess_StopsEverySessionAndEveryTokenOfThePerson_AndNobodyElses(
        UserAccessEnd kind, UserStatus expected)
    {
        var user = CreateUser();
        var other = CreateUser(id: Guid.CreateVersion7(), email: "other@example.com");
        SetupUsers(user, other);
        var s1 = CreateSession();
        var s2 = CreateSession();
        var colleagueSession = CreateSession(userId: other.Id);
        SetupSessions(s1, s2, colleagueSession);
        var t1 = Token(user.Id, Guid.CreateVersion7(), "h1");
        var t2 = Token(user.Id, Guid.CreateVersion7(), "h2");
        var colleagueToken = Token(other.Id, Guid.CreateVersion7(), "h3");
        SetupApiTokens(t1, t2, colleagueToken);

        var found = await Sut().EndAccessAsync(user.Id, Admin, kind, "left the company", CancellationToken.None);

        found.Should().BeTrue();
        user.Status.Should().Be(expected);
        s1.Status.Should().Be(SessionStatus.Revoked);
        s2.Status.Should().Be(SessionStatus.Revoked);
        colleagueSession.Status.Should().Be(SessionStatus.Active);
        t1.Status.Should().Be(ApiTokenStatus.Revoked);
        t2.Status.Should().Be(ApiTokenStatus.Revoked);
        t1.RevokedReason.Should().Be(ApiTokenRevokedReason.UserDisabled);
        t1.RevokedBy.Should().Be(Admin);
        colleagueToken.Status.Should().Be(ApiTokenStatus.Active);
        other.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task EndAccess_SetsTheUserWatermark_SoIssuedAccessTokensAndRealtimeStop()
    {
        var user = CreateUser();
        SetupUsers(user);
        SetupSessions();
        SetupApiTokens();

        await Sut().EndAccessAsync(user.Id, Admin, UserAccessEnd.Deactivated, null, CancellationToken.None);

        JwtBlacklistMock.Verify(
            b => b.RevokeUserBeforeAsync(user.Id, TestNow, It.Is<TimeSpan>(t => t >= TimeSpan.FromHours(1))),
            Times.Once);
    }

    [Fact]
    public async Task EndAccess_RunTwice_ChangesNothingMore_AndFinishesLeftoverRevocation()
    {
        var user = CreateUser();
        SetupUsers(user);
        var session = CreateSession();
        SetupSessions(session);
        SetupApiTokens();
        await Sut().EndAccessAsync(user.Id, Admin, UserAccessEnd.Deactivated, null, CancellationToken.None);
        var version = user.Version;

        var leftover = CreateSession();
        SetupSessions(session, leftover);
        await Sut().EndAccessAsync(user.Id, Admin, UserAccessEnd.Deactivated, null, CancellationToken.None);

        user.Version.Should().Be(version, "disabling a disabled person is a no-op");
        leftover.Status.Should().Be(SessionStatus.Revoked);
    }

    [Fact]
    public async Task EndAccess_ForAPersonWhoDoesNotExist_ReturnsFalse_AndWritesNoWatermark()
    {
        SetupUsers();

        var found = await Sut().EndAccessAsync(Guid.CreateVersion7(), Admin, UserAccessEnd.Deactivated, null, CancellationToken.None);

        found.Should().BeFalse();
        JwtBlacklistMock.Verify(
            b => b.RevokeUserBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAfterEndAccess_IsRefused_BecauseItsSessionIsRevoked()
    {
        var user = CreateUser();
        SetupUsers(user);
        var session = CreateSession();
        SetupSessions(session);
        SetupApiTokens();

        await Sut().EndAccessAsync(user.Id, Admin, UserAccessEnd.Deactivated, null, CancellationToken.None);

        session.Status.Should().Be(SessionStatus.Revoked, "RefreshToken refuses any session that is not Active");
    }
}
