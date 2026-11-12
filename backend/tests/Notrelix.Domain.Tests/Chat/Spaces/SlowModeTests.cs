using FluentAssertions;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Chat.Spaces.Events;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Domain.Tests.Chat.Spaces;

/// <summary>UC-CHT-H08: a channel's slow mode, the seconds between two messages of one person.</summary>
public class SlowModeTests
{
    private readonly Guid _manager = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic) =>
        Space.CreateChannel(Guid.NewGuid(), Guid.NewGuid(), kind, "general", null, null, 1, _manager, _now);

    [Fact]
    public void ANewChannelHasNoSlowMode()
    {
        Channel().SlowModeSeconds.Should().Be(0);
    }

    [Fact]
    public void SettingItBumpsTheVersionAndRaisesOneFact_AndTheSameValueChangesNothing()
    {
        var channel = Channel();
        var version = channel.Version;

        channel.SetSlowMode(30, _manager, _now).Should().BeTrue();

        channel.SlowModeSeconds.Should().Be(30);
        channel.Version.Should().Be(version + 1);
        channel.DomainEvents.OfType<ChannelSlowModeChangedDomainEvent>().Should().ContainSingle().Which.SpaceId.Should().Be(channel.Id);

        channel.SetSlowMode(30, _manager, _now).Should().BeFalse();
        channel.Version.Should().Be(version + 1);
        channel.DomainEvents.OfType<ChannelSlowModeChangedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void ZeroTurnsItOff()
    {
        var channel = Channel();
        channel.SetSlowMode(30, _manager, _now);

        channel.SetSlowMode(0, _manager, _now).Should().BeTrue();

        channel.SlowModeSeconds.Should().Be(0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ChatLimits.SlowModeMaxSeconds + 1)]
    public void AValueOutsideTheRangeIsRefused(int seconds)
    {
        var channel = Channel();

        var act = () => channel.SetSlowMode(seconds, _manager, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SlowModeInvalid);
        channel.SlowModeSeconds.Should().Be(0);
    }

    [Fact]
    public void AnArchivedChannelTakesNoChange()
    {
        var channel = Channel();
        channel.SetArchived(true, _manager, _now);

        var act = () => channel.SetSlowMode(10, _manager, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void AConversationHasNoSlowMode()
    {
        var group = Space.CreateGroup(Guid.NewGuid(), Guid.NewGuid(), "ours", [_manager, Guid.NewGuid(), Guid.NewGuid()], _manager, _now);

        var act = () => group.SetSlowMode(10, _manager, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SlowModeInvalid);
    }
}
