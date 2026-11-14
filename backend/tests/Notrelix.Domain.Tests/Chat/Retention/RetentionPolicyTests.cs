using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Chat.Spaces.Events;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Domain.Tests.Chat.Retention;

/// <summary>UC-CHT-H05: the Workspace's retention rule and the way a message expires.</summary>
public class RetentionPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _account = Guid.NewGuid();
    private readonly Guid _workspace = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();

    private RetentionPolicy Policy() => RetentionPolicy.CreateDefault(_account, _workspace, Now);

    [Fact]
    public void ADefaultPolicyHasNoRetention_AndItsIdIsTheWorkspaceId()
    {
        var policy = Policy();

        policy.IsEnabled.Should().BeFalse();
        policy.RetentionDays.Should().BeNull();
        policy.Mode.Should().Be(RetentionMode.Tombstone);
        policy.Id.Should().Be(_workspace);
        policy.Version.Should().Be(1);
        policy.CutoffAt(Now).Should().BeNull();
    }

    [Fact]
    public void SettingAWindowBumpsTheVersion_AndTheCutoffIsNowMinusTheWindow()
    {
        var policy = Policy();

        policy.Change(90, RetentionMode.Purge, _owner, Now).Should().BeTrue();

        policy.RetentionDays.Should().Be(90);
        policy.Mode.Should().Be(RetentionMode.Purge);
        policy.IsEnabled.Should().BeTrue();
        policy.Version.Should().Be(2);
        policy.UpdatedBy.Should().Be(_owner);
        policy.CutoffAt(Now).Should().Be(Now.AddDays(-90));
    }

    [Fact]
    public void TheSameRuleChangesNothing()
    {
        var policy = Policy();
        policy.Change(30, RetentionMode.Tombstone, _owner, Now);

        policy.Change(30, RetentionMode.Tombstone, _owner, Now).Should().BeFalse();

        policy.Version.Should().Be(2);
    }

    [Fact]
    public void ANullWindowTurnsRetentionOff_AndKeepsTheChosenMode()
    {
        var policy = Policy();
        policy.Change(30, RetentionMode.Purge, _owner, Now);

        policy.Change(null, RetentionMode.Tombstone, _owner, Now).Should().BeTrue();

        policy.IsEnabled.Should().BeFalse();
        policy.Mode.Should().Be(RetentionMode.Purge, "the mode the Owner chose stays for the next time");
        policy.CutoffAt(Now).Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ChatLimits.RetentionMinDays - 1)]
    [InlineData(ChatLimits.RetentionMaxDays + 1)]
    [InlineData(-5)]
    public void AWindowOutsideTheLimitsIsRefused(int days)
    {
        var policy = Policy();

        var act = () => policy.Change(days, RetentionMode.Tombstone, _owner, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.RetentionInvalid);
        policy.RetentionDays.Should().BeNull();
        policy.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(ChatLimits.RetentionMinDays)]
    [InlineData(ChatLimits.RetentionMaxDays)]
    public void TheLimitsThemselvesAreAllowed(int days)
    {
        Policy().Change(days, RetentionMode.Tombstone, _owner, Now).Should().BeTrue();
    }

    [Fact]
    public void AnUnknownModeIsRefused()
    {
        var act = () => Policy().Change(30, (RetentionMode)99, _owner, Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.RetentionInvalid);
    }

    [Theory]
    [InlineData("tombstone", RetentionMode.Tombstone)]
    [InlineData("purge", RetentionMode.Purge)]
    public void TheModeRoundTripsItsWireName(string wire, RetentionMode mode)
    {
        RetentionModeWire.FromWire(wire).Should().Be(mode);
        mode.ToWire().Should().Be(wire);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Purge")]
    [InlineData(null)]
    public void AnUnknownWireModeIsNull(string? wire) => RetentionModeWire.FromWire(wire).Should().BeNull();

    // ---- how a message expires ------------------------------------------------------------------

    private Message Text(string content = "old words") =>
        Message.CreateUserMessage(_account, _workspace, Guid.NewGuid(), 1, Guid.NewGuid(), content, "c-1", null, Now.AddDays(-100));

    [Fact]
    public void AnExpiredMessageBecomesATombstoneWithNoOneAsItsDeleter_AndRaisesNoFactOfItsOwn()
    {
        var message = Text();
        var version = message.Version;

        message.ExpireByRetention(Now).Should().BeTrue();

        message.IsDeleted.Should().BeTrue();
        message.Content.Should().BeNull();
        message.DeletedBy.Should().BeNull();
        message.IsRemovedByModerator.Should().BeFalse("it is not a moderator's removal");
        message.Sequence.Should().Be(1);
        message.Version.Should().Be(version + 1);
        message.DomainEvents.OfType<Notrelix.Domain.Chat.Messages.Events.MessageDeletedDomainEvent>().Should().BeEmpty("the job announces a batch with one fact per space");
    }

    [Fact]
    public void ATombstoneIsNotExpiredTwice()
    {
        var message = Text();
        message.ExpireByRetention(Now);
        var version = message.Version;

        message.ExpireByRetention(Now).Should().BeFalse();

        message.Version.Should().Be(version);
    }

    [Fact]
    public void ASystemMessageIsNeverExpired()
    {
        var system = Message.CreateSystemMessage(_account, _workspace, Guid.NewGuid(), 2, Guid.NewGuid(), "x joined", Now.AddDays(-100));

        var act = () => system.ExpireByRetention(Now);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SystemMessageImmutable);
    }

    [Fact]
    public void ASpaceAnnouncesAnAppliedBatchWithoutChanging()
    {
        var space = Space.CreateChannel(_account, _workspace, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now);
        var version = space.Version;

        space.RecordRetentionApplied(Now);

        space.Version.Should().Be(version);
        space.DomainEvents.OfType<ChannelRetentionAppliedDomainEvent>().Should().ContainSingle().Which.SpaceId.Should().Be(space.Id);
    }

    [Fact]
    public void ADeletedSpaceIsNotAnnounced()
    {
        var space = Space.CreateChannel(_account, _workspace, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now);
        space.Delete(_owner, Now);

        var act = () => space.RecordRetentionApplied(Now);

        act.Should().Throw<BusinessRuleException>();
    }
}
