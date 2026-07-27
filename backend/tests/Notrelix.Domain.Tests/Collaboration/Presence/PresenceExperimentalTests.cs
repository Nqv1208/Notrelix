using FluentAssertions;
using Notrelix.Domain.Collaboration.Presence;

namespace Notrelix.Domain.Tests.Collaboration.Presence;

public class PresenceExperimentalTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var session = PresenceSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        session.Status.Should().Be(PresenceStatus.Online);
        session.LastSeenAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateHeartbeat_ShouldUpdateLastSeen()
    {
        var session = PresenceSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var later = DateTimeOffset.UtcNow.AddSeconds(30);

        session.UpdateHeartbeat(later);

        session.LastSeenAt.Should().Be(later);
        session.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void GoOffline_ShouldSetStatus()
    {
        var session = PresenceSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var offlineAt = DateTimeOffset.UtcNow.AddMinutes(5);

        session.GoOffline(offlineAt);

        session.Status.Should().Be(PresenceStatus.Offline);
        session.LastSeenAt.Should().Be(offlineAt);
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => PresenceSession.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => PresenceSession.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        var act = () => PresenceSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
    }
}
