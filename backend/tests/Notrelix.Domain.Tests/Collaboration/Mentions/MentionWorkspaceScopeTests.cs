using FluentAssertions;
using Notrelix.Domain.Collaboration.Mentions;

namespace Notrelix.Domain.Tests.Collaboration;

public class MentionWorkspaceScopeTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    [Fact]
    public void Create_WithMatchingWorkspace_ShouldSucceed()
    {
        var source = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), WsA);
        var mention = Mention.Create(Guid.NewGuid(), WsA, source, MentionType.User, Guid.NewGuid(), DateTimeOffset.UtcNow);
        mention.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void Create_WithMismatchedWorkspace_ShouldThrow()
    {
        var source = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), WsB);
        var act = () => Mention.Create(Guid.NewGuid(), WsA, source, MentionType.User, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithUnscopedResourceRef_ShouldSucceed()
    {
        var source = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid());
        var mention = Mention.Create(Guid.NewGuid(), WsA, source, MentionType.User, Guid.NewGuid(), DateTimeOffset.UtcNow);
        mention.WorkspaceId.Should().Be(WsA);
    }
}
