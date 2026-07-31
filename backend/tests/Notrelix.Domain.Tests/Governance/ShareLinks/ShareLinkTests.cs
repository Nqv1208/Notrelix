using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Governance.ShareLinks;

namespace Notrelix.Domain.Tests.Governance;

[CoversAggregate(typeof(ShareLink))]
public class ShareLinkTests
{
    [Fact]
    public void Create_ShouldHashToken_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var tokenHash = ShareLinkTokenHash.Create("secret-token-123");
        var createdBy = Guid.NewGuid();

        var link = ShareLink.Create(Guid.NewGuid(), workspaceId, ResourceType.Page, resourceId, tokenHash, ShareLinkAccessMode.WorkspaceOnly, createdBy, DateTimeOffset.UtcNow);

        link.TokenHash.Hash.Should().NotBe("secret-token-123");
        link.Status.Should().Be(ShareLinkStatus.Active);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkCreatedDomainEvent);
    }

    [CoversMutation(typeof(ShareLink), nameof(ShareLink.Expire), MutationScenario.Valid, typeof(DateTimeOffset))]
    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpirationPassed()
    {
        var workspaceId = Guid.NewGuid();
        var link = ShareLink.Create(Guid.NewGuid(), workspaceId, ResourceType.Board, Guid.NewGuid(), ShareLinkTokenHash.Create("token"), ShareLinkAccessMode.Public, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(-10));

        link.IsExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [CoversMutation(typeof(ShareLink), nameof(ShareLink.Disable), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disable_ShouldSetStatusToDisabled_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var link = ShareLink.Create(Guid.NewGuid(), workspaceId, ResourceType.Board, Guid.NewGuid(), ShareLinkTokenHash.Create("token"), ShareLinkAccessMode.WorkspaceOnly, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)link).ClearDomainEvents();

        var disabledBy = Guid.NewGuid();
        link.Disable(disabledBy, DateTimeOffset.UtcNow);

        link.Status.Should().Be(ShareLinkStatus.Disabled);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkDisabledDomainEvent);
    }

    [CoversMutation(typeof(ShareLink), nameof(ShareLink.RotateTokenHash), MutationScenario.Event, typeof(ShareLinkTokenHash), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void RotateTokenHash_ShouldUpdateHash_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var link = ShareLink.Create(Guid.NewGuid(), workspaceId, ResourceType.Board, Guid.NewGuid(), ShareLinkTokenHash.Create("token1"), ShareLinkAccessMode.WorkspaceOnly, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)link).ClearDomainEvents();

        var newHash = ShareLinkTokenHash.Create("token2");
        var rotatedBy = Guid.NewGuid();
        link.RotateTokenHash(newHash, rotatedBy, DateTimeOffset.UtcNow);

        link.TokenHash.Should().Be(newHash);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkRotatedDomainEvent);
    }
}
