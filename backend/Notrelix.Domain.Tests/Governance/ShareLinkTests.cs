using FluentAssertions;
using Notrelix.Domain.Governance;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Governance.ShareLinks;
using Xunit;

namespace Notrelix.Domain.Tests.Governance;

public class ShareLinkTests
{
    [Fact]
    public void Create_ShouldHashToken_AndRaiseEvent()
    {
        var resourceId = Guid.NewGuid();
        var tokenHash = ShareLinkTokenHash.Create("secret-token-123");
        var createdBy = Guid.NewGuid();

        var link = ShareLink.Create(ResourceType.Page, resourceId, tokenHash, ShareLinkAccessMode.Public, createdBy);

        link.TokenHash.Hash.Should().NotBe("secret-token-123");
        link.Status.Should().Be(ShareLinkStatus.Active);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkCreatedEvent);
    }

    [Fact]
    public void IsExpired_ShouldReturnTrue_WhenExpirationPassed()
    {
        var link = ShareLink.Create(ResourceType.Board, Guid.NewGuid(), ShareLinkTokenHash.Create("token"), ShareLinkAccessMode.Public, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-10));
        
        link.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldSetStatusToDisabled_AndRaiseEvent()
    {
        var link = ShareLink.Create(ResourceType.Board, Guid.NewGuid(), ShareLinkTokenHash.Create("token"), ShareLinkAccessMode.Public, Guid.NewGuid());
        link.ClearDomainEvents();

        var disabledBy = Guid.NewGuid();
        link.Disable(disabledBy);

        link.Status.Should().Be(ShareLinkStatus.Disabled);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkDisabledEvent);
    }

    [Fact]
    public void RotateTokenHash_ShouldUpdateHash_AndRaiseEvent()
    {
        var link = ShareLink.Create(ResourceType.Board, Guid.NewGuid(), ShareLinkTokenHash.Create("token1"), ShareLinkAccessMode.Public, Guid.NewGuid());
        link.ClearDomainEvents();

        var newHash = ShareLinkTokenHash.Create("token2");
        var rotatedBy = Guid.NewGuid();
        link.RotateTokenHash(newHash, rotatedBy);

        link.TokenHash.Should().Be(newHash);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkRotatedEvent);
    }
}
