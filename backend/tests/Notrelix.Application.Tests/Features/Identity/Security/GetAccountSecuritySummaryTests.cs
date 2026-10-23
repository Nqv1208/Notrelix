using Notrelix.Application.Features.Identity.Security.Queries.GetAccountSecuritySummary;
using Notrelix.Domain.Identity.OAuth;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Security;

public class GetAccountSecuritySummaryTests : IdentityHandlerTestBase
{
    private GetAccountSecuritySummaryQueryHandler CreateSut() => new(
        IdentityContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_CountsOnlyOwnActiveUnexpiredSessions()
    {
        SetupUsers(CreateUser());
        SetupSessions(
            CreateSession(),
            CreateSession(),
            CreateSession(status: SessionStatus.Revoked),
            UserSession.Create(TestUserId, RefreshTokenHash.Create("expired"), TestNow.AddMinutes(-1), TestNow.AddDays(-2)),
            CreateSession(userId: Guid.CreateVersion7()));

        var result = await CreateSut().Handle(new GetAccountSecuritySummaryQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.ActiveSessionCount.Should().Be(2);
        result.Data.ServerTime.Should().Be(TestNow);
        result.Data.HasPasswordCredential.Should().BeTrue();
        result.Data.Mfa.IsEnabled.Should().BeFalse();
        result.Data.PasswordChangedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReportsPasswordChangedAtOfTheCallerOnly()
    {
        SetupUsers(CreateUser());
        var mine = UserSecuritySettings.Create(TestUserId, TestNow.AddDays(-10));
        mine.MarkPasswordChanged(TestNow.AddDays(-3));
        var theirs = UserSecuritySettings.Create(Guid.CreateVersion7(), TestNow.AddDays(-10));
        theirs.MarkPasswordChanged(TestNow.AddDays(-1));
        SetupUserSecuritySettings(theirs, mine);

        var result = await CreateSut().Handle(new GetAccountSecuritySummaryQuery(), CancellationToken.None);

        result.Data!.PasswordChangedAt.Should().Be(TestNow.AddDays(-3));
    }

    [Fact]
    public async Task Handle_ListsProviderNamesWithoutSubjectIds()
    {
        SetupUsers(CreateUser());
        var snapshot = OAuthProfileSnapshot.Create(OAuthProvider.Google, 1, JsonValue.EmptyObject());
        IdentityContextMock.Setup(c => c.OAuthAccounts).Returns(CreateAsyncDbSet(new List<OAuthAccount>
        {
            OAuthAccount.Create(TestUserId, OAuthProvider.Google, "subject-123", snapshot),
            OAuthAccount.Create(Guid.CreateVersion7(), OAuthProvider.GitHub, "other-subject", snapshot)
        }));

        var result = await CreateSut().Handle(new GetAccountSecuritySummaryQuery(), CancellationToken.None);

        result.Data!.LinkedProviders.Should().Equal("Google");
        System.Text.Json.JsonSerializer.Serialize(result.Data).Should().NotContain("subject-123");
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoUserRow_ReturnsNotFound()
    {
        SetupUsers();

        var result = await CreateSut().Handle(new GetAccountSecuritySummaryQuery(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
    }
}
