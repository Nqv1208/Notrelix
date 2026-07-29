using FluentAssertions;
using Notrelix.Domain.Collaboration.Presence;
using Notrelix.Domain.Tests.Freeze;

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

    [Fact]
    public void DomainCapabilityRegistry_ShouldClassifyPresenceAsExperimental()
    {
        var type = typeof(PresenceSession);
        var status = DomainCapabilityRegistry.ResolveCapability(type);
        status.Should().Be(DomainCapabilityStatus.Experimental);
    }

    [Fact]
    public void DomainCapabilityRegistry_ShouldClassifyCommentAsFrozen()
    {
        var type = typeof(Notrelix.Domain.Collaboration.Comments.Comment);
        var status = DomainCapabilityRegistry.ResolveCapability(type);
        status.Should().Be(DomainCapabilityStatus.Frozen);
    }

    [Fact]
    public void DomainCapabilityRegistry_ShouldPreferLongestPrefix()
    {
        var presenceStatus = DomainCapabilityRegistry.ResolveCapability(typeof(PresenceSession));
        var commentStatus = DomainCapabilityRegistry.ResolveCapability(
            typeof(Notrelix.Domain.Collaboration.Comments.Comment));
        presenceStatus.Should().Be(DomainCapabilityStatus.Experimental);
        commentStatus.Should().Be(DomainCapabilityStatus.Frozen);
    }
}
