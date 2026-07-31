using FluentAssertions;
using Notrelix.Domain.Collaboration;
using Notrelix.Domain.Collaboration.Reactions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Collaboration.Reactions;

public class ReactionDuplicateTests
{
    [Fact]
    public void Create_WithDuplicateCheck_Passes_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), workspaceId);
        var userId = Guid.NewGuid();
        var emoji = Emoji.Create("+1");

        var reaction = Reaction.Create(Guid.NewGuid(), workspaceId, target, userId, emoji, DateTimeOffset.UtcNow,
            _ => false);

        reaction.Should().NotBeNull();
        reaction.DomainEvents.Should().ContainSingle(e => e is ReactionCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithDuplicateCheck_Fails_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), workspaceId);
        var userId = Guid.NewGuid();
        var emoji = Emoji.Create("+1");

        var act = () => Reaction.Create(Guid.NewGuid(), workspaceId, target, userId, emoji, DateTimeOffset.UtcNow,
            _ => true);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(CollaborationRuleCodes.Collaboration_Reaction_DuplicateReaction);
    }

    [Fact]
    public void Create_WithDifferentUser_ShouldNotBeDuplicate()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), workspaceId);
        var checkArgs = new List<Guid>();

        Reaction.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Emoji.Create("+1"), DateTimeOffset.UtcNow,
            userId =>
            {
                checkArgs.Add(userId);
                return false;
            });

        checkArgs.Should().ContainSingle();
    }

    [Fact]
    public void Create_WithDuplicateCheck_Null_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var target = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), workspaceId);

        var reaction = Reaction.Create(Guid.NewGuid(), workspaceId, target, Guid.NewGuid(), Emoji.Create("heart"),
            DateTimeOffset.UtcNow, null);

        reaction.Should().NotBeNull();
        reaction.DomainEvents.Should().ContainSingle(e => e is ReactionCreatedDomainEvent);
    }

    [CoversMutation(typeof(Reaction), nameof(Reaction.Remove), MutationScenario.Event, typeof(DateTimeOffset))]
    [Fact]
    public void Remove_ShouldRaiseEvent()
    {
        var reaction = Reaction.Create(Guid.NewGuid(), Guid.NewGuid(),
            ResourceRef.Create(ResourceType.Comment, Guid.NewGuid()), Guid.NewGuid(),
            Emoji.Create("rocket"), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)reaction).ClearDomainEvents();

        reaction.Remove(DateTimeOffset.UtcNow);

        reaction.DomainEvents.Should().ContainSingle(e => e is ReactionRemovedDomainEvent);
    }

    [CoversMutation(typeof(Reaction), nameof(Reaction.Remove), MutationScenario.Invalid, typeof(DateTimeOffset))]
    [Fact]
    public void Remove_ShouldNotThrowOnMultipleCalls()
    {
        var reaction = Reaction.Create(Guid.NewGuid(), Guid.NewGuid(),
            ResourceRef.Create(ResourceType.Comment, Guid.NewGuid()), Guid.NewGuid(),
            Emoji.Create("rocket"), DateTimeOffset.UtcNow);
        reaction.Remove(DateTimeOffset.UtcNow);

        var act = () => reaction.Remove(DateTimeOffset.UtcNow);

        act.Should().NotThrow();
    }
}
