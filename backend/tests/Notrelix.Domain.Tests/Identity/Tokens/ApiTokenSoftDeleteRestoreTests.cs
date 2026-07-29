using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity;

public class ApiTokenSoftDeleteRestoreTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(ApiToken), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var token = ApiToken.Create(Guid.NewGuid(), workspaceId, _actorId, "My Token", "hash", null, _actorId, _now);
        var version = token.Version;

        token.SoftDelete(_actorId, _now);

        token.IsDeleted.Should().BeTrue();
        token.Version.Should().Be(version + 1);
        token.DomainEvents.Should().ContainSingle(e => e is ApiTokenSoftDeletedDomainEvent);
        var evt = (ApiTokenSoftDeletedDomainEvent)token.DomainEvents.Single(e => e is ApiTokenSoftDeletedDomainEvent);
        evt.TokenId.Should().Be(token.Id);
    }

    [CoversMutation(typeof(ApiToken), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var token = ApiToken.Create(Guid.NewGuid(), workspaceId, _actorId, "My Token", "hash", null, _actorId, _now);
        token.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)token).ClearDomainEvents();
        var version = token.Version;

        token.Restore(_actorId, _now);

        token.IsDeleted.Should().BeFalse();
        token.Version.Should().Be(version + 1);
        token.DomainEvents.Should().ContainSingle(e => e is ApiTokenRestoredDomainEvent);
    }
}
