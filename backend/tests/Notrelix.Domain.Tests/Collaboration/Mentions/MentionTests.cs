using FluentAssertions;
using Notrelix.Domain.Collaboration.Mentions;
using Notrelix.Domain.Collaboration.Mentions.Events;

namespace Notrelix.Domain.Tests.Collaboration;

public class MentionTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceKind.Create("collaboration.comment"), Guid.NewGuid(), workspaceId);

        var mention = Mention.Create(Guid.NewGuid(), workspaceId, source, MentionType.User, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        mention.WorkspaceId.Should().Be(workspaceId);
        mention.Source.Should().Be(source);
        mention.Type.Should().Be(MentionType.User);
        mention.MentionedId.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithDifferentMentionTypes_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceKind.Create("documents.page"), Guid.NewGuid(), workspaceId);

        var teamMention = Mention.Create(Guid.NewGuid(), workspaceId, source, MentionType.Team, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var pageMention = Mention.Create(Guid.NewGuid(), workspaceId, source, MentionType.Page, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var boardMention = Mention.Create(Guid.NewGuid(), workspaceId, source, MentionType.Board, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        teamMention.Type.Should().Be(MentionType.Team);
        pageMention.Type.Should().Be(MentionType.Page);
        boardMention.Type.Should().Be(MentionType.Board);
    }

    [Fact]
    public void Create_WithWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceKind.Create("collaboration.comment"), Guid.NewGuid(), Guid.NewGuid());

        var act = () => Mention.Create(Guid.NewGuid(), workspaceId, source, MentionType.User, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyMentionedId_ShouldThrow()
    {
        var act = () => Mention.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceRef.Create(ResourceKind.Create("documents.page"), Guid.NewGuid()), MentionType.User, Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyMentioner_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceKind.Create("collaboration.comment"), Guid.NewGuid(), workspaceId);

        var act = () => Mention.Create(Guid.NewGuid(), workspaceId, source, MentionType.User, Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>(
            "M2F requires the trusted creator/mentioner actor to be captured at mention creation");
    }

    [Fact]
    public void Create_ShouldRaiseEvent_CarryingDistinctMentionerAndMentionedFacts()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), Guid.NewGuid(), workspaceId);
        var mentionedId = Guid.NewGuid();
        var mentionerId = Guid.NewGuid();

        var mention = Mention.Create(accountId, workspaceId, source, MentionType.User, mentionedId, mentionerId, DateTimeOffset.UtcNow);

        mention.MentionedByUserId.Should().Be(mentionerId);
        mention.DomainEvents
            .OfType<MentionCreatedDomainEvent>()
            .Should().ContainSingle().Which.Should().Match<MentionCreatedDomainEvent>(e =>
                e.AccountId == accountId
                && e.WorkspaceId == workspaceId
                && e.MentionId == mention.Id
                && e.Source == source
                && e.MentionedId == mentionedId
                && e.MentionedByUserId == mentionerId);
    }
}
