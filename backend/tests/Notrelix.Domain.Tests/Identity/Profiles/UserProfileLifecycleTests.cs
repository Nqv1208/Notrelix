using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserProfileLifecycleTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldRaiseCreatedEvent()
    {
        var profile = UserProfile.Create(_actorId, _now);

        profile.DomainEvents.Should().ContainSingle(e => e is UserProfileCreatedDomainEvent);
        var evt = (UserProfileCreatedDomainEvent)profile.DomainEvents.Single(e => e is UserProfileCreatedDomainEvent);
        evt.UserProfileId.Should().Be(profile.Id);
        evt.UserId.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now);
    }
}
