using FluentAssertions;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Domain.Tests.Chat.Spaces;

/// <summary>Notification level and mute per person and space (CHT-019, UC-CHT-D05, D06).</summary>
public class MembershipNotificationTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _creator = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private Membership NewMembership()
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _creator, _now);
        return space.GetActiveMembership(_creator)!;
    }

    [Fact]
    public void AdvanceReadSequence_MovesForwardOnly()
    {
        var membership = NewMembership();

        membership.AdvanceReadSequence(4).Should().BeTrue();
        membership.AdvanceReadSequence(2).Should().BeFalse("a lower sequence is a no-op");
        membership.AdvanceReadSequence(4).Should().BeFalse("the same sequence is a no-op");
        membership.ReadSequence.Should().Be(4);
    }

    [Fact]
    public void A_NewMembership_IsNotifiedForEverythingAndNotMuted()
    {
        var membership = NewMembership();

        membership.NotificationLevel.Should().Be(NotificationLevel.All);
        membership.ReadSequence.Should().Be(0);
        membership.MentionCount.Should().Be(0);
        membership.MutedUntil.Should().BeNull();
        membership.IsMuted(_now).Should().BeFalse();
    }

    [Fact]
    public void SetNotificationLevel_StoresTheLevel_AndASecondSameCallChangesNothing()
    {
        var membership = NewMembership();

        membership.SetNotificationLevel(NotificationLevel.Mentions, null, _now).Should().BeTrue();
        membership.SetNotificationLevel(NotificationLevel.Mentions, null, _now).Should().BeFalse();
        membership.NotificationLevel.Should().Be(NotificationLevel.Mentions);
    }

    [Fact]
    public void A_TimedMute_IsNoneUntilATime_AndMutesUntilThen()
    {
        var membership = NewMembership();
        var until = _now.AddHours(2);

        membership.SetNotificationLevel(NotificationLevel.None, until, _now).Should().BeTrue();

        membership.NotificationLevel.Should().Be(NotificationLevel.None);
        membership.MutedUntil.Should().Be(until);
        membership.IsMuted(_now.AddHours(1)).Should().BeTrue();
        membership.IsMuted(until).Should().BeFalse("the mute ends at its until time");
    }

    [Fact]
    public void AnExpiredMute_ReadsAsAll_EvenBeforeTheStoredLevelIsChanged()
    {
        var membership = NewMembership();
        membership.SetNotificationLevel(NotificationLevel.None, _now.AddMinutes(30), _now);

        membership.EffectiveNotificationLevel(_now.AddHours(1)).Should().Be(NotificationLevel.All);
    }

    [Fact]
    public void AMuteWithAnUntilNotAfterNow_IsANoOp()
    {
        var membership = NewMembership();

        membership.SetNotificationLevel(NotificationLevel.None, _now.AddMinutes(-5), _now).Should().BeFalse();
        membership.SetNotificationLevel(NotificationLevel.None, _now, _now).Should().BeFalse();

        membership.NotificationLevel.Should().Be(NotificationLevel.All);
        membership.MutedUntil.Should().BeNull();
    }

    [Fact]
    public void A_PermanentMute_HasNoUntil_AndStaysMuted()
    {
        var membership = NewMembership();

        membership.SetNotificationLevel(NotificationLevel.None, null, _now).Should().BeTrue();

        membership.IsMuted(_now.AddYears(5)).Should().BeTrue();
    }

    [Fact]
    public void ChangingToALevelOtherThanNone_ClearsTheUntil()
    {
        var membership = NewMembership();
        membership.SetNotificationLevel(NotificationLevel.None, _now.AddHours(1), _now);

        membership.SetNotificationLevel(NotificationLevel.All, null, _now).Should().BeTrue();

        membership.MutedUntil.Should().BeNull();
        membership.IsMuted(_now).Should().BeFalse();
    }

    [Fact]
    public void AnUntilWithALevelOtherThanNone_IsRefused()
    {
        var membership = NewMembership();

        var act = () => membership.SetNotificationLevel(NotificationLevel.Mentions, _now.AddHours(1), _now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ALeftMember_HasNoActiveMembership()
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _creator, _now);
        var guest = Guid.NewGuid();
        space.AddMember(guest, MembershipRole.Member, _now);
        space.RemoveMember(guest, _now.AddMinutes(1));

        space.GetActiveMembership(guest).Should().BeNull();
    }
}
