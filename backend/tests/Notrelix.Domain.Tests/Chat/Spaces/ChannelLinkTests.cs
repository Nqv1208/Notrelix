using FluentAssertions;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Chat.Spaces.Events;
using Notrelix.Domain.Common.Exceptions;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Domain.Tests.Chat.Spaces;

/// <summary>UC-CHT-H10: a channel holds at most one link to a Board, Page or Team, by reference.</summary>
public class ChannelLinkTests
{
    private readonly Guid _manager = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private Space Channel() => Space.CreateChannel(Guid.NewGuid(), Guid.NewGuid(), SpaceKind.ChannelPublic, "general", null, null, 1, _manager, _now);

    [Fact]
    public void ANewChannelHasNoLink()
    {
        var channel = Channel();

        channel.HasLink.Should().BeFalse();
        channel.LinkKind.Should().BeNull();
        channel.LinkId.Should().BeNull();
    }

    [Fact]
    public void SettingTheLinkBumpsTheVersionAndRaisesOneFact_AndTheSameLinkChangesNothing()
    {
        var channel = Channel();
        var board = Guid.NewGuid();
        var version = channel.Version;

        channel.SetLink(ChannelLinkKinds.Board, board, _manager, _now).Should().BeTrue();

        channel.LinkKind.Should().Be(ChannelLinkKinds.Board);
        channel.LinkId.Should().Be(board);
        channel.Version.Should().Be(version + 1);
        channel.DomainEvents.OfType<ChannelLinkChangedDomainEvent>().Should().ContainSingle().Which.SpaceId.Should().Be(channel.Id);

        channel.SetLink(ChannelLinkKinds.Board, board, _manager, _now).Should().BeFalse();
        channel.Version.Should().Be(version + 1);
        channel.DomainEvents.OfType<ChannelLinkChangedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void AnotherLinkReplacesTheFirst()
    {
        var channel = Channel();
        channel.SetLink(ChannelLinkKinds.Board, Guid.NewGuid(), _manager, _now);
        var page = Guid.NewGuid();

        channel.SetLink(ChannelLinkKinds.Page, page, _manager, _now).Should().BeTrue();

        channel.LinkKind.Should().Be(ChannelLinkKinds.Page);
        channel.LinkId.Should().Be(page);
    }

    [Fact]
    public void ClearingRemovesTheLink_AndClearingNothingChangesNothing()
    {
        var channel = Channel();
        channel.ClearLink(_manager, _now).Should().BeFalse();
        channel.SetLink(ChannelLinkKinds.Team, Guid.NewGuid(), _manager, _now);
        var version = channel.Version;

        channel.ClearLink(_manager, _now).Should().BeTrue();

        channel.HasLink.Should().BeFalse();
        channel.Version.Should().Be(version + 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("work-management.board-item")]
    [InlineData("anything")]
    public void AnUnknownKindIsRefused(string kind)
    {
        var act = () => Channel().SetLink(kind, Guid.NewGuid(), _manager, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LinkInvalid);
    }

    [Fact]
    public void AnEmptyIdIsRefused()
    {
        var act = () => Channel().SetLink(ChannelLinkKinds.Page, Guid.Empty, _manager, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LinkInvalid);
    }

    [Fact]
    public void AnArchivedChannelCannotBeLinked()
    {
        var channel = Channel();
        channel.SetArchived(true, _manager, _now);

        var act = () => channel.SetLink(ChannelLinkKinds.Page, Guid.NewGuid(), _manager, _now);

        act.Should().Throw<BusinessRuleException>();
    }
}
