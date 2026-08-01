using FluentAssertions;
using Notrelix.Domain.Collaboration.Presence;

namespace Notrelix.Domain.Tests.Collaboration.Presence;

public class PresenceIsolationTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var session = PresenceSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        session.Status.Should().Be(PresenceStatus.Online);
    }

    [Fact]
    public void UpdateHeartbeat_ShouldUpdateTimestamp()
    {
        var session = PresenceSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var later = DateTimeOffset.UtcNow.AddMinutes(1);
        session.UpdateHeartbeat(later);
        session.LastSeenAt.Should().Be(later);
    }

    [Fact]
    public void GoOffline_ShouldSetOffline()
    {
        var session = PresenceSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        session.GoOffline(DateTimeOffset.UtcNow);
        session.Status.Should().Be(PresenceStatus.Offline);
    }
}
