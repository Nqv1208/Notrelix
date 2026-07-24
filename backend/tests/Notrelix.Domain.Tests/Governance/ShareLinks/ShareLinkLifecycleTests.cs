using FluentAssertions;
using Notrelix.Domain.Governance.ShareLinks;

namespace Notrelix.Domain.Tests.Governance.ShareLinks;

public class ShareLinkLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void ShareLink_SoftDelete_ShouldRaiseEvent()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.ClearDomainEvents();
        var version = link.Version;

        link.SoftDelete(Actor, Now);

        link.IsDeleted.Should().BeTrue();
        link.Version.Should().Be(version + 1);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkSoftDeletedDomainEvent);
        var evt = (ShareLinkSoftDeletedDomainEvent)link.DomainEvents.Single(e => e is ShareLinkSoftDeletedDomainEvent);
        evt.LinkId.Should().Be(link.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void ShareLink_Restore_ShouldRaiseEvent()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.SoftDelete(Actor, Now);
        link.ClearDomainEvents();
        var version = link.Version;

        link.Restore(Actor, Now);

        link.IsDeleted.Should().BeFalse();
        link.Version.Should().Be(version + 1);
        link.DomainEvents.Should().ContainSingle(e => e is ShareLinkRestoredDomainEvent);
        var evt = (ShareLinkRestoredDomainEvent)link.DomainEvents.Single(e => e is ShareLinkRestoredDomainEvent);
        evt.LinkId.Should().Be(link.Id);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void ShareLink_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.SoftDelete(Actor, Now);
        link.ClearDomainEvents();
        var version = link.Version;

        link.SoftDelete(Actor, Now);

        link.Version.Should().Be(version);
        link.DomainEvents.Should().NotContain(e => e is ShareLinkSoftDeletedDomainEvent);
    }

    [Fact]
    public void ShareLink_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var tokenHash = ShareLinkTokenHash.Create("test-hash");
        var link = ShareLink.Create(Guid.NewGuid(), WsA, ResourceType.Board, Guid.NewGuid(), tokenHash, ShareLinkAccessMode.WorkspaceOnly, Actor, Now);
        link.ClearDomainEvents();
        var version = link.Version;

        link.Restore(Actor, Now);

        link.Version.Should().Be(version);
        link.DomainEvents.Should().NotContain(e => e is ShareLinkRestoredDomainEvent);
    }
}
