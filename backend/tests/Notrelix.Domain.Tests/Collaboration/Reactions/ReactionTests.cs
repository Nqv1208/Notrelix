using FluentAssertions;
using Notrelix.Domain.Collaboration.Reactions;

namespace Notrelix.Domain.Tests.Collaboration;

public class ReactionTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceKind.Create("collaboration.comment"), Guid.NewGuid(), workspaceId);
        var userId = Guid.NewGuid();
        var emoji = Emoji.Create("+1");

        var reaction = Reaction.Create(Guid.NewGuid(), workspaceId, target, userId, emoji, DateTimeOffset.UtcNow);

        reaction.WorkspaceId.Should().Be(workspaceId);
        reaction.Target.Should().Be(target);
        reaction.UserId.Should().Be(userId);
        reaction.Emoji.Should().Be(emoji);
        reaction.DomainEvents.Should().ContainSingle(e => e is ReactionCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceKind.Create("collaboration.comment"), Guid.NewGuid(), Guid.NewGuid());

        var act = () => Reaction.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Emoji.Create("heart"), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Remove_ShouldRaiseEvent()
    {
        var reaction = CreateReaction();
        ((IHasDomainEvents)reaction).ClearDomainEvents();

        reaction.Remove(DateTimeOffset.UtcNow);

        reaction.DomainEvents.Should().ContainSingle(e => e is ReactionRemovedDomainEvent);
    }

    private static Reaction CreateReaction()
    {
        var workspaceId = Guid.NewGuid();
        return Reaction.Create(Guid.NewGuid(), workspaceId, ResourceRef.Create(ResourceKind.Create("collaboration.comment"), Guid.NewGuid(), workspaceId), Guid.NewGuid(), Emoji.Create("rocket"), DateTimeOffset.UtcNow);
    }
}
