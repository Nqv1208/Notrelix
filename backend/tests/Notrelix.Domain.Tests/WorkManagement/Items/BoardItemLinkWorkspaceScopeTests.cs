using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardItemLinkWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var link = BoardItemLink.Create(Guid.NewGuid(), WsA, Guid.NewGuid(), Guid.NewGuid(), target, BoardItemLinkType.Reference, null, DateTimeOffset.UtcNow);
        link.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => BoardItemLink.Create(Guid.NewGuid(), WsA, Guid.NewGuid(), Guid.NewGuid(), target, BoardItemLinkType.Reference, null, DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var link = BoardItemLink.Create(Guid.NewGuid(), WsA, Guid.NewGuid(), Guid.NewGuid(), target, BoardItemLinkType.Reference, null, DateTimeOffset.UtcNow);
        link.WorkspaceId.Should().Be(WsA);
    }
}
