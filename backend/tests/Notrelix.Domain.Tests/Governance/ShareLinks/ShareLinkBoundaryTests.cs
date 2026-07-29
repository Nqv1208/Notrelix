using FluentAssertions;
using Notrelix.Domain.Governance.ShareLinks;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Governance.ShareLinks;

public class ShareLinkBoundaryTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void ShareLink_Create_WithPublicAccessAndNoExpiry_ShouldThrow()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var act = () => ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.Public, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*expiration*");
    }

    [Fact]
    public void ShareLink_Create_WithPublicAccessAndExpiry_ShouldSucceed()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.Public, Actor, Now, Now.AddDays(7));
        link.Should().NotBeNull();
        link.AccessMode.Should().Be(ShareLinkAccessMode.Public);
        link.ExpiresAt.Should().Be(Now.AddDays(7));
    }

    [Fact]
    public void ShareLink_Create_WithWorkspaceOnlyAccess_ShouldAllowNoExpiry()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.Should().NotBeNull();
        link.ExpiresAt.Should().BeNull();
    }

    [CoversMutation(typeof(ShareLink), "Expire(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ShareLink_Expire_ShouldUseNullActor()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.Expire(Now);
        link.Status.Should().Be(ShareLinkStatus.Expired);
    }

    [CoversMutation(typeof(ShareLink), "Expire(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ShareLink_IsExpired_WhenExpired_ShouldReturnTrue()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.Expire(Now);
        link.IsExpired(Now).Should().BeTrue();
    }

    [CoversMutation(typeof(ShareLink), "Expire(System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void ShareLink_IsExpired_WhenPastExpiry_ShouldReturnTrue()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now, Now.AddDays(-1));
        link.IsExpired(Now).Should().BeTrue();
    }
}
