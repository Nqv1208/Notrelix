using FluentAssertions;
using Notrelix.Domain.WorkManagement.Approvals;

namespace Notrelix.Domain.Tests.WorkManagement.Approvals;

public class ApprovalRequestEventTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void ApprovalRequest_Delete_ShouldRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve this", Actor, Now);
        ((IHasDomainEvents)request).ClearDomainEvents();
        var version = request.Version;

        request.Delete(Actor, Now);

        request.IsDeleted.Should().BeTrue();
        request.Version.Should().Be(version + 1);
        request.DomainEvents.Should().ContainSingle(e => e is ApprovalRequestDeletedDomainEvent);
    }

    [Fact]
    public void ApprovalRequest_Delete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve this", Actor, Now);
        request.Delete(Actor, Now);
        ((IHasDomainEvents)request).ClearDomainEvents();
        var version = request.Version;

        request.Delete(Actor, Now);

        request.Version.Should().Be(version);
        request.DomainEvents.Should().NotContain(e => e is ApprovalRequestDeletedDomainEvent);
    }

    [Fact]
    public void ApprovalRequest_Restore_ShouldRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve this", Actor, Now);
        request.Delete(Actor, Now);
        ((IHasDomainEvents)request).ClearDomainEvents();
        var version = request.Version;

        request.Restore(Actor, Now);

        request.IsDeleted.Should().BeFalse();
        request.Version.Should().Be(version + 1);
        request.DomainEvents.Should().ContainSingle(e => e is ApprovalRequestRestoredDomainEvent);
    }

    [Fact]
    public void ApprovalRequest_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve this", Actor, Now);
        ((IHasDomainEvents)request).ClearDomainEvents();
        var version = request.Version;

        request.Restore(Actor, Now);

        request.Version.Should().Be(version);
        request.DomainEvents.Should().NotContain(e => e is ApprovalRequestRestoredDomainEvent);
    }
}
