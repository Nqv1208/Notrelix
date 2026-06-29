using FluentAssertions;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Domain.Tests.WorkManagement;

public class BoardItemLinkTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var sourceItemId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), workspaceId);

        var link = BoardItemLink.Create(workspaceId, boardId, sourceItemId, target, BoardItemLinkType.Reference, Guid.NewGuid(), DateTimeOffset.UtcNow);

        link.WorkspaceId.Should().Be(workspaceId);
        link.BoardId.Should().Be(boardId);
        link.SourceItemId.Should().Be(sourceItemId);
        link.Target.Should().Be(target);
        link.LinkType.Should().Be(BoardItemLinkType.Reference);
    }

    [Fact]
    public void Create_WhenTargetWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());

        var act = () => BoardItemLink.Create(workspaceId, Guid.NewGuid(), Guid.NewGuid(), target, BoardItemLinkType.Reference, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Create_WithNullTarget_ShouldThrow()
    {
        var act = () => BoardItemLink.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!, BoardItemLinkType.Reference, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());
        var act = () => BoardItemLink.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), target, BoardItemLinkType.Reference, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyBoardId_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());
        var act = () => BoardItemLink.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), target, BoardItemLinkType.Reference, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptySourceItemId_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), Guid.NewGuid());
        var act = () => BoardItemLink.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, target, BoardItemLinkType.Reference, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WhenTargetHasNoWorkspace_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.External, Guid.NewGuid());

        var link = BoardItemLink.Create(workspaceId, boardId, Guid.NewGuid(), target, BoardItemLinkType.Reference, Guid.NewGuid(), DateTimeOffset.UtcNow);

        link.Target.Should().Be(target);
    }
}
