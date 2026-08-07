using FluentAssertions;
using Notrelix.Domain.WorkManagement.Approvals;

namespace Notrelix.Domain.Tests.WorkManagement;

public class ApprovalRequestWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve", Guid.NewGuid(), DateTimeOffset.UtcNow);
        request.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WsB);
        var act = () => ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid());
        var request = ApprovalRequest.Create(Guid.NewGuid(), WsA, target, "Approve", Guid.NewGuid(), DateTimeOffset.UtcNow);
        request.WorkspaceId.Should().Be(WsA);
    }
}
