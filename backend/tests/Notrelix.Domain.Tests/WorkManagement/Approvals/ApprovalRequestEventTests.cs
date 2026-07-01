using FluentAssertions;
using Notrelix.Domain.WorkManagement.Approvals;

namespace Notrelix.Domain.Tests.WorkManagement.Approvals;

public class ApprovalRequestEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void ApprovalRequest_SoftDelete_ShouldRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve this", Actor, Now);
        request.ClearDomainEvents();
        var version = request.Version;

        request.SoftDelete(Actor, Now);

        request.IsDeleted.Should().BeTrue();
        request.Version.Should().Be(version + 1);
        request.DomainEvents.Should().ContainSingle(e => e is ApprovalRequestSoftDeletedDomainEvent);
    }

    [Fact]
    public void ApprovalRequest_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve this", Actor, Now);
        request.SoftDelete(Actor, Now);
        request.ClearDomainEvents();
        var version = request.Version;

        request.SoftDelete(Actor, Now);

        request.Version.Should().Be(version);
        request.DomainEvents.Should().NotContain(e => e is ApprovalRequestSoftDeletedDomainEvent);
    }

    [Fact]
    public void ApprovalRequest_Restore_ShouldRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve this", Actor, Now);
        request.SoftDelete(Actor, Now);
        request.ClearDomainEvents();
        var version = request.Version;

        request.Restore(Actor, Now);

        request.IsDeleted.Should().BeFalse();
        request.Version.Should().Be(version + 1);
        request.DomainEvents.Should().ContainSingle(e => e is ApprovalRequestRestoredDomainEvent);
    }

    [Fact]
    public void ApprovalRequest_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve this", Actor, Now);
        request.ClearDomainEvents();
        var version = request.Version;

        request.Restore(Actor, Now);

        request.Version.Should().Be(version);
        request.DomainEvents.Should().NotContain(e => e is ApprovalRequestRestoredDomainEvent);
    }
}
