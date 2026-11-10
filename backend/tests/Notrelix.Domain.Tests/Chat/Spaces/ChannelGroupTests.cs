using FluentAssertions;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Tests.Chat.Spaces;

public class ChannelGroupTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actor = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_TrimsTheName()
    {
        var group = ChannelGroup.Create(_accountId, _workspaceId, "  Engineering ", 1, _actor, _now);

        group.Name.Should().Be("Engineering");
        group.WorkspaceId.Should().Be(_workspaceId);
    }

    [Fact]
    public void Create_RejectsANameLongerThanTheLimit()
    {
        var act = () => ChannelGroup.Create(_accountId, _workspaceId, new string('g', ChatLimits.ChannelGroupNameMaxLength + 1), 1, _actor, _now);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Create_RejectsANonFinitePosition()
    {
        var act = () => ChannelGroup.Create(_accountId, _workspaceId, "Ops", double.NaN, _actor, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.InvalidPosition);
    }

    [Fact]
    public void Update_ChangesTheVersionOnlyWhenSomethingChanges()
    {
        var group = ChannelGroup.Create(_accountId, _workspaceId, "Ops", 1, _actor, _now);
        var before = group.Version;

        group.Update("Ops", 1, _actor, _now).Should().BeFalse();
        group.Version.Should().Be(before);

        group.Update("Platform", 2, _actor, _now).Should().BeTrue();
        group.Name.Should().Be("Platform");
        group.Position.Should().Be(2);
        group.Version.Should().BeGreaterThan(before);
    }
}
