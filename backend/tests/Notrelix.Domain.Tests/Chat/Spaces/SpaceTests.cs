using FluentAssertions;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Domain.Tests.Chat.Spaces;

public class SpaceTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _creator = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private Space NewChannel(SpaceKind kind = SpaceKind.ChannelPublic, string name = "general") =>
        Space.CreateChannel(_accountId, _workspaceId, kind, name, null, null, 1, _creator, _now);

    [Fact]
    public void CreateChannel_MakesTheCreatorTheFirstManager()
    {
        var channel = NewChannel();

        channel.IsActiveManager(_creator).Should().BeTrue();
        channel.ActiveManagerCount().Should().Be(1);
        channel.Memberships.Should().ContainSingle(m => m.UserId == _creator && m.Role == MembershipRole.Manager);
    }

    [Theory]
    [InlineData(SpaceKind.Direct)]
    [InlineData(SpaceKind.Group)]
    public void CreateChannel_RejectsKindsOutsideThisPhase(SpaceKind kind)
    {
        var act = () => Space.CreateChannel(_accountId, _workspaceId, kind, "x", null, null, 1, _creator, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.KindNotSupported);
    }

    [Fact]
    public void CreateChannel_TrimsTheNameAndDropsABlankTopic()
    {
        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "  design  ", "   ", null, 1, _creator, _now);

        channel.Name.Should().Be("design");
        channel.Topic.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateChannel_RejectsABlankName(string name)
    {
        var act = () => NewChannel(name: name);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void CreateChannel_RejectsANameLongerThanTheLimit()
    {
        var act = () => NewChannel(name: new string('a', ChatLimits.ChannelNameMaxLength + 1));

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void CreateChannel_AcceptsANameAtTheLimit()
    {
        var channel = NewChannel(name: new string('a', ChatLimits.ChannelNameMaxLength));

        channel.Name.Should().HaveLength(ChatLimits.ChannelNameMaxLength);
    }

    [Fact]
    public void UpdateDetails_ChangesTheVersionOnlyWhenSomethingChanges()
    {
        var channel = NewChannel();
        var before = channel.Version;

        channel.UpdateDetails("general", null, _creator, _now).Should().BeFalse();
        channel.Version.Should().Be(before);

        channel.UpdateDetails("random", "Off topic", _creator, _now).Should().BeTrue();
        channel.Version.Should().BeGreaterThan(before);
        channel.Topic.Should().Be("Off topic");
    }

    [Fact]
    public void UpdateDetails_RejectsATopicLongerThanTheLimit()
    {
        var channel = NewChannel();

        var act = () => channel.UpdateDetails("general", new string('t', ChatLimits.ChannelTopicMaxLength + 1), _creator, _now);

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Archived_ChannelIsReadOnly()
    {
        var channel = NewChannel();
        channel.SetArchived(true, _creator, _now);

        var rename = () => channel.UpdateDetails("renamed", null, _creator, _now);
        var addMember = () => channel.AddMember(Guid.NewGuid(), MembershipRole.Member, _now);
        var leave = () => channel.RemoveMember(_creator, _now);

        rename.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.ChannelArchived);
        addMember.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.ChannelArchived);
        leave.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.ChannelArchived);
    }

    [Fact]
    public void Unarchive_ReopensTheChannel()
    {
        var channel = NewChannel();
        channel.SetArchived(true, _creator, _now);

        channel.SetArchived(false, _creator, _now).Should().BeTrue();

        channel.IsArchived.Should().BeFalse();
        channel.UpdateDetails("renamed", null, _creator, _now).Should().BeTrue();
    }

    [Fact]
    public void RemoveMember_CannotRemoveTheLastManager()
    {
        var channel = NewChannel();
        var other = Guid.NewGuid();
        channel.AddMember(other, MembershipRole.Member, _now);

        var act = () => channel.RemoveMember(_creator, _now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.LastManager);
        channel.IsActiveManager(_creator).Should().BeTrue();
    }

    [Fact]
    public void RemoveMember_AllowsALeaveWhenAnotherManagerRemains()
    {
        var channel = NewChannel();
        var second = Guid.NewGuid();
        channel.AddMember(second, MembershipRole.Manager, _now);

        channel.RemoveMember(_creator, _now).Should().BeTrue();

        channel.IsActiveMember(_creator).Should().BeFalse();
        channel.ActiveManagerCount().Should().Be(1);
    }

    [Fact]
    public void RemoveMember_ReturnsFalseForSomeoneWhoIsNotAMember()
    {
        var channel = NewChannel();

        channel.RemoveMember(Guid.NewGuid(), _now).Should().BeFalse();
    }

    [Fact]
    public void AddMember_ReturnsFalseWhenAlreadyActive()
    {
        var channel = NewChannel();
        var member = Guid.NewGuid();
        channel.AddMember(member, MembershipRole.Member, _now).Should().BeTrue();

        channel.AddMember(member, MembershipRole.Member, _now).Should().BeFalse();
    }

    [Fact]
    public void AddMember_RejoinsAFormerMember()
    {
        var channel = NewChannel();
        var member = Guid.NewGuid();
        channel.AddMember(member, MembershipRole.Member, _now);
        channel.RemoveMember(member, _now.AddHours(1));

        channel.AddMember(member, MembershipRole.Manager, _now.AddHours(2)).Should().BeTrue();

        var membership = channel.GetActiveMembership(member);
        membership.Should().NotBeNull();
        membership!.Role.Should().Be(MembershipRole.Manager);
        channel.Memberships.Should().ContainSingle(m => m.UserId == member);
    }

    [Fact]
    public void Delete_MarksTheChannelDeleted()
    {
        var channel = NewChannel();

        channel.Delete(_creator, _now);

        channel.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void MoveTo_ReportsNoChangeWhenPlacementIsTheSame()
    {
        var channel = NewChannel();
        var groupId = Guid.NewGuid();
        channel.MoveTo(groupId, 2, _creator, _now).Should().BeTrue();

        channel.MoveTo(groupId, 2, _creator, _now).Should().BeFalse();
        channel.RemoveFromGroup(_creator, _now).Should().BeTrue();
        channel.GroupId.Should().BeNull();
    }
}
