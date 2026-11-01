using Notrelix.Application.Features.Identity.Sessions.Queries.GetUserSessions;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Sessions;

public class GetUserSessionsQueryTests : IdentityHandlerTestBase
{
    private GetUserSessionsQueryHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        DateTimeProviderMock.Object);

    [Fact]
    public async Task TwoLiveDevices_BothShow_AndOnlyTheCallersIsMarkedCurrent()
    {
        var mine = CreateSession();
        var other = CreateSession();
        RequestContextMock.Setup(c => c.SessionId).Returns(mine.Id);
        SetupSessions(mine, other);

        var result = await CreateSut().Handle(new GetUserSessionsQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Should().HaveCount(2);
        result.Data!.Single(s => s.IsCurrent).Id.Should().Be(mine.Id);
        result.Data!.Single(s => !s.IsCurrent).Id.Should().Be(other.Id);
    }

    [Fact]
    public async Task AnotherPersonsSessions_NeverAppear()
    {
        var mine = CreateSession();
        var theirs = CreateSession(userId: Guid.CreateVersion7());
        var theirsEnded = CreateSession(userId: Guid.CreateVersion7(), status: SessionStatus.Revoked);
        RequestContextMock.Setup(c => c.SessionId).Returns(mine.Id);
        SetupSessions(mine, theirs, theirsEnded);

        var result = await CreateSut().Handle(new GetUserSessionsQuery(), CancellationToken.None);

        result.Data!.Select(s => s.Id).Should().Equal(mine.Id);
    }

    [Fact]
    public async Task EndedSessions_AreCappedAtTwenty_WhileEveryLiveOneStays()
    {
        var live = Enumerable.Range(0, 3).Select(_ => CreateSession()).ToArray();
        var ended = Enumerable.Range(0, GetUserSessionsQueryHandler.EndedSessionLimit + 5)
            .Select(_ => CreateSession(status: SessionStatus.Revoked)).ToArray();
        SetupSessions(live.Concat(ended).ToArray());

        var result = await CreateSut().Handle(new GetUserSessionsQuery(), CancellationToken.None);

        result.Data!.Count(s => s.Status == SessionStatus.Active).Should().Be(3);
        result.Data!.Count(s => s.Status == SessionStatus.Revoked).Should().Be(GetUserSessionsQueryHandler.EndedSessionLimit);
    }

    [Fact]
    public async Task ASessionPastItsEndTimeThatNothingClosed_ReadsAsExpired()
    {
        var stale = CreateSession(); // expires TestNow + 30 days
        SetupSessions(stale);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow.AddDays(31));

        var result = await CreateSut().Handle(new GetUserSessionsQuery(), CancellationToken.None);

        result.Data!.Should().ContainSingle().Which.Status.Should().Be(SessionStatus.Expired);
    }

    [Fact]
    public async Task WithNoSessionBound_NoRowIsMarkedCurrent()
    {
        var one = CreateSession();
        SetupSessions(one);
        RequestContextMock.Setup(c => c.SessionId).Returns((Guid?)null);

        var result = await CreateSut().Handle(new GetUserSessionsQuery(), CancellationToken.None);

        result.Data!.Should().ContainSingle().Which.IsCurrent.Should().BeFalse();
    }
}
