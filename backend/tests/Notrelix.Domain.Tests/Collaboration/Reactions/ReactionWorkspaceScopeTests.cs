using FluentAssertions;
using Notrelix.Domain.Collaboration.Reactions;

namespace Notrelix.Domain.Tests.Collaboration;

public class ReactionWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WsA);
        var reaction = Reaction.Create(Guid.NewGuid(), WsA, target, Guid.NewGuid(), Emoji.Create("+1"), DateTimeOffset.UtcNow);
        reaction.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), WsB);
        var act = () => Reaction.Create(Guid.NewGuid(), WsA, target, Guid.NewGuid(), Emoji.Create("+1"), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid());
        var reaction = Reaction.Create(Guid.NewGuid(), WsA, target, Guid.NewGuid(), Emoji.Create("+1"), DateTimeOffset.UtcNow);
        reaction.WorkspaceId.Should().Be(WsA);
    }
}
