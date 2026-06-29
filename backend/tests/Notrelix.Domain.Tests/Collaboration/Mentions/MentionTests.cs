using FluentAssertions;
using Notrelix.Domain.Collaboration.Mentions;

namespace Notrelix.Domain.Tests.Collaboration;

public class MentionTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), workspaceId);

        var mention = Mention.Create(workspaceId, source, MentionType.User, Guid.NewGuid(), DateTimeOffset.UtcNow);

        mention.WorkspaceId.Should().Be(workspaceId);
        mention.Source.Should().Be(source);
        mention.Type.Should().Be(MentionType.User);
        mention.MentionedId.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithDifferentMentionTypes_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Page, Guid.NewGuid(), workspaceId);

        var teamMention = Mention.Create(workspaceId, source, MentionType.Team, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var pageMention = Mention.Create(workspaceId, source, MentionType.Page, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var boardMention = Mention.Create(workspaceId, source, MentionType.Board, Guid.NewGuid(), DateTimeOffset.UtcNow);

        teamMention.Type.Should().Be(MentionType.Team);
        pageMention.Type.Should().Be(MentionType.Page);
        boardMention.Type.Should().Be(MentionType.Board);
    }

    [Fact]
    public void Create_WithWorkspaceMismatch_ShouldThrow()
    {
        var workspaceId = Guid.NewGuid();
        var source = ResourceRef.Create(ResourceType.Comment, Guid.NewGuid(), Guid.NewGuid());

        var act = () => Mention.Create(workspaceId, source, MentionType.User, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<WorkspaceMismatchException>();
    }

    [Fact]
    public void Create_WithEmptyMentionedId_ShouldThrow()
    {
        var act = () => Mention.Create(Guid.NewGuid(), ResourceRef.Create(ResourceType.Page, Guid.NewGuid()), MentionType.User, Guid.Empty, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }
}
