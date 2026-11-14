using FluentAssertions;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Permissions.Events;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Domain.Tests.Chat.Permissions;

/// <summary>UC-CHT-H01 (overrides) and UC-CHT-H03 (moderator tombstone) in the domain: the resolver matrix and the invariants.</summary>
public class ChannelPermissionTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private readonly Guid _moderator = Guid.NewGuid();
    private readonly DateTimeOffset _now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic) =>
        Space.CreateChannel(_accountId, _workspaceId, kind, "announcements", null, null, 1, _manager, _now);

    private static ChannelPermissionEntry Deny(ChannelPermissionSubject subject, ChannelAction action) =>
        new(subject, action, ChannelPermissionEffect.Deny);

    private static ChannelPermissionEntry Allow(ChannelPermissionSubject subject, ChannelAction action) =>
        new(subject, action, ChannelPermissionEffect.Allow);

    // ---- Resolver -----------------------------------------------------------------------------

    [Fact]
    public void WithoutOverrides_EveryoneKeepsTheDefault()
    {
        foreach (var action in Enum.GetValues<ChannelAction>())
        {
            ChannelPermissionResolver.IsAllowed([], action, isWorkspaceAdmin: false, isChannelManager: false, isGuest: false).Should().BeTrue();
            ChannelPermissionResolver.IsAllowed([], action, isWorkspaceAdmin: false, isChannelManager: true, isGuest: false).Should().BeTrue();
            ChannelPermissionResolver.IsAllowed([], action, isWorkspaceAdmin: false, isChannelManager: false, isGuest: true).Should().BeTrue();
        }
    }

    [Theory]
    // subject of the deny, then the person: admin, manager, guest
    [InlineData(ChannelPermissionSubject.Member, false, false, false, false)]
    [InlineData(ChannelPermissionSubject.Member, false, true, false, true)]
    [InlineData(ChannelPermissionSubject.Member, false, false, true, true)]
    [InlineData(ChannelPermissionSubject.Member, true, false, false, true)]
    [InlineData(ChannelPermissionSubject.Guest, false, false, true, false)]
    [InlineData(ChannelPermissionSubject.Guest, false, false, false, true)]
    [InlineData(ChannelPermissionSubject.Manager, false, true, false, false)]
    [InlineData(ChannelPermissionSubject.Manager, false, false, false, true)]
    [InlineData(ChannelPermissionSubject.Manager, true, true, false, true)]
    public void ADenyReachesOnlyItsOwnRole_AndNeverAWorkspaceAdmin(
        ChannelPermissionSubject denied, bool admin, bool manager, bool guest, bool expected)
    {
        var overrides = new[] { Deny(denied, ChannelAction.Post) };

        ChannelPermissionResolver.IsAllowed(overrides, ChannelAction.Post, admin, manager, guest).Should().Be(expected);
        ChannelPermissionResolver.IsAllowed(overrides, ChannelAction.React, admin, manager, guest).Should().BeTrue("another action is untouched");
    }

    [Fact]
    public void AnAllowChangesNothing_BecauseAnOverrideNeverExceedsTheRoleDefault()
    {
        var overrides = new[] { Allow(ChannelPermissionSubject.Member, ChannelAction.Post) };

        ChannelPermissionResolver.IsAllowed(overrides, ChannelAction.Post, false, false, false).Should().BeTrue();
    }

    [Fact]
    public void SubjectOf_MatchesTheManagerBeforeTheGuestAndNeverAnAdmin()
    {
        ChannelPermissionResolver.SubjectOf(true, true, true).Should().BeNull();
        ChannelPermissionResolver.SubjectOf(false, true, true).Should().Be(ChannelPermissionSubject.Manager);
        ChannelPermissionResolver.SubjectOf(false, false, true).Should().Be(ChannelPermissionSubject.Guest);
        ChannelPermissionResolver.SubjectOf(false, false, false).Should().Be(ChannelPermissionSubject.Member);
    }

    // ---- Normalize: deny beats allow, lockout -------------------------------------------------

    [Fact]
    public void Normalize_ADenyBeatsAnAllowForTheSameRoleAndAction_InEitherOrder()
    {
        var allowFirst = ChannelPermissionResolver.Normalize(
            [Allow(ChannelPermissionSubject.Member, ChannelAction.Post), Deny(ChannelPermissionSubject.Member, ChannelAction.Post)]);
        var denyFirst = ChannelPermissionResolver.Normalize(
            [Deny(ChannelPermissionSubject.Member, ChannelAction.Post), Allow(ChannelPermissionSubject.Member, ChannelAction.Post)]);

        allowFirst.Should().ContainSingle().Which.Effect.Should().Be(ChannelPermissionEffect.Deny);
        denyFirst.Should().ContainSingle().Which.Effect.Should().Be(ChannelPermissionEffect.Deny);
    }

    [Fact]
    public void Normalize_KeepsOneEntryPerRoleAndActionInAStableOrder()
    {
        var result = ChannelPermissionResolver.Normalize(
        [
            Deny(ChannelPermissionSubject.Guest, ChannelAction.Pin),
            Deny(ChannelPermissionSubject.Member, ChannelAction.Post),
            Deny(ChannelPermissionSubject.Member, ChannelAction.Post),
        ]);

        result.Should().Equal(
            Deny(ChannelPermissionSubject.Member, ChannelAction.Post),
            Deny(ChannelPermissionSubject.Guest, ChannelAction.Pin));
    }

    [Fact]
    public void Normalize_RefusesToDenyAManagerInviting()
    {
        var act = () => ChannelPermissionResolver.Normalize([Deny(ChannelPermissionSubject.Manager, ChannelAction.Invite)]);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.PermissionLockout);
    }

    // ---- Space aggregate ----------------------------------------------------------------------

    [Fact]
    public void SetPermissionOverrides_ReplacesTheSetBumpsTheVersionAndRaisesOneFact()
    {
        var channel = Channel();
        var version = channel.Version;

        channel.SetPermissionOverrides(
            [Deny(ChannelPermissionSubject.Member, ChannelAction.Post), Deny(ChannelPermissionSubject.Guest, ChannelAction.Post)], _manager, _now)
            .Should().BeTrue();

        channel.PermissionOverrides.Should().HaveCount(2);
        channel.Version.Should().Be(version + 1);
        channel.DomainEvents.OfType<ChannelPermissionsChangedDomainEvent>().Should().ContainSingle()
            .Which.SpaceId.Should().Be(channel.Id);

        channel.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.React)], _manager, _now).Should().BeTrue();

        channel.PermissionEntries().Should().Equal(Deny(ChannelPermissionSubject.Member, ChannelAction.React));
    }

    [Fact]
    public void SetPermissionOverrides_FlipsAnExistingEntryInPlace()
    {
        var channel = Channel();
        channel.SetPermissionOverrides([Allow(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, _now);

        channel.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, _now).Should().BeTrue();

        channel.PermissionOverrides.Should().ContainSingle().Which.Effect.Should().Be(ChannelPermissionEffect.Deny);
    }

    [Fact]
    public void SetPermissionOverrides_WithTheStoredSetChangesNothing()
    {
        var channel = Channel();
        channel.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, _now);
        var version = channel.Version;
        var events = channel.DomainEvents.Count;

        channel.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, _now).Should().BeFalse();

        channel.Version.Should().Be(version);
        channel.DomainEvents.Should().HaveCount(events);
    }

    [Fact]
    public void SetPermissionOverrides_WithAnEmptySetClears()
    {
        var channel = Channel();
        channel.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, _now);

        channel.SetPermissionOverrides([], _manager, _now).Should().BeTrue();

        channel.PermissionOverrides.Should().BeEmpty();
    }

    [Fact]
    public void SetPermissionOverrides_RefusesAnArchivedChannelAndAConversation()
    {
        var archived = Channel();
        archived.SetArchived(true, _manager, _now);
        var direct = Space.CreateDirect(_accountId, _workspaceId, _manager, _moderator, _now);

        var onArchived = () => archived.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, _now);
        var onDirect = () => direct.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, _now);

        onArchived.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.ChannelArchived);
        onDirect.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.KindNotSupported);
    }

    [Fact]
    public void TheManagerIsNeverLockedOutOfManagingTheChannel_EvenWithEveryOtherRoleDenied()
    {
        var channel = Channel();
        var every = Enum.GetValues<ChannelAction>()
            .Where(a => a != ChannelAction.Invite)
            .SelectMany(a => new[] { Deny(ChannelPermissionSubject.Member, a), Deny(ChannelPermissionSubject.Guest, a) });
        channel.SetPermissionOverrides(every.Append(Deny(ChannelPermissionSubject.Member, ChannelAction.Invite)), _manager, _now);

        // Managing the channel (rename, archive, members, overrides) is not an overridable action, and an admin or manager is not a subject
        // of the member and guest denials. The manager keeps inviting because denying it is refused.
        ChannelPermissionResolver.IsAllowed(channel.PermissionEntries(), ChannelAction.Invite, false, true, false).Should().BeTrue();
        ChannelPermissionResolver.IsAllowed(channel.PermissionEntries(), ChannelAction.Post, true, false, false).Should().BeTrue();
    }

    [Fact]
    public void Wire_RoundTripsEveryValueAndRejectsUnknownText()
    {
        foreach (var action in Enum.GetValues<ChannelAction>())
            ChannelPermissionWire.ActionFromWire(action.ToWire()).Should().Be(action);
        foreach (var subject in Enum.GetValues<ChannelPermissionSubject>())
            ChannelPermissionWire.SubjectFromWire(subject.ToWire()).Should().Be(subject);
        foreach (var effect in Enum.GetValues<ChannelPermissionEffect>())
            ChannelPermissionWire.EffectFromWire(effect.ToWire()).Should().Be(effect);

        ChannelPermissionWire.SubjectFromWire("admin").Should().BeNull("a Workspace admin is never a subject");
        ChannelPermissionWire.ActionFromWire("manage").Should().BeNull();
        ChannelPermissionWire.EffectFromWire("maybe").Should().BeNull();
    }

    // ---- Moderator delete (UC-CHT-H03) --------------------------------------------------------

    private Message Text(Space channel, Guid author) =>
        Message.CreateUserMessage(_accountId, _workspaceId, channel.Id, 1, author, "secret words", "c-1", null, _now);

    [Fact]
    public void DeleteAsModerator_LeavesATombstoneMarkedAsRemovedByAModerator()
    {
        var channel = Channel();
        var author = Guid.NewGuid();
        var message = Text(channel, author);

        message.DeleteAsModerator(_moderator, _now).Should().BeTrue();

        message.IsDeleted.Should().BeTrue();
        message.Content.Should().BeNull("the content goes, as with an author's delete");
        message.AuthorId.Should().Be(author);
        message.Sequence.Should().Be(1);
        message.DeletedBy.Should().Be(_moderator);
        message.IsRemovedByModerator.Should().BeTrue();
        message.DomainEvents.OfType<Notrelix.Domain.Chat.Messages.Events.MessageDeletedDomainEvent>().Should().ContainSingle()
            .Which.DeletedBy.Should().Be(_moderator);
    }

    [Fact]
    public void DeleteAsModerator_OfAnAlreadyDeletedMessageChangesNothing()
    {
        var message = Text(Channel(), Guid.NewGuid());
        message.DeleteAsModerator(_moderator, _now);
        var version = message.Version;

        message.DeleteAsModerator(_moderator, _now).Should().BeFalse();

        message.Version.Should().Be(version);
    }

    [Fact]
    public void DeleteAsModerator_RefusesYourOwnMessageAndASystemMessage()
    {
        var channel = Channel();
        var own = Text(channel, _moderator);
        var system = Message.CreateSystemMessage(_accountId, _workspaceId, channel.Id, 2, _manager, "joined the channel", _now);

        var onOwn = () => own.DeleteAsModerator(_moderator, _now);
        var onSystem = () => system.DeleteAsModerator(_moderator, _now);

        onOwn.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SelfTarget);
        onSystem.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(ChatRuleCodes.SystemMessageImmutable);
    }

    [Fact]
    public void AnAuthorsOwnDelete_IsNotMarkedAsRemovedByAModerator()
    {
        var author = Guid.NewGuid();
        var message = Text(Channel(), author);

        message.Delete(author, _now);

        message.IsRemovedByModerator.Should().BeFalse();
    }
}
