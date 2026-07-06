using FluentAssertions;
using Notrelix.Domain.Collaboration.Comments;

namespace Notrelix.Domain.Tests.Collaboration;

public class CommentWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var comment = Comment.Create(Guid.NewGuid(), WsA, target, "ok", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsB);
        var act = () => Comment.Create(Guid.NewGuid(), WsA, target, "bad", Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid());
        var comment = Comment.Create(Guid.NewGuid(), WsA, target, "ok", Guid.NewGuid(), DateTimeOffset.UtcNow);
        comment.WorkspaceId.Should().Be(WsA);
    }
}
