using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Messaging;
using FluentValidation.TestHelper;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.ReadState.DTOs;
using Notrelix.Application.Features.Chat.ReadState.Commands.MarkAllRead;
using Notrelix.Application.Features.Chat.ReadState.Commands.MarkSpaceRead;
using Notrelix.Application.Features.Chat.ReadState.Commands.SetNotificationLevel;
using Notrelix.Application.Features.Chat.ReadState.Queries.GetUnreadSummary;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat;

/// <summary>
/// Read state use cases UC-CHT-D01 to D07 at handler level, with fakes. The boundary rule is the same as the store's
/// (monotonic, bounded by the last sequence); the SQL itself is proved against PostgreSQL in the integration tests.
/// </summary>
public class ReadStateHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _otherWorkspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IIntegrationEventCollector> _events = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly FakeChatReadStateStore _store;

    public ReadStateHandlerTests()
    {
        _store = new FakeChatReadStateStore(_spaces);

        _clock.Setup(c => c.UtcNow).Returns(() => Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _context.Setup(c => c.UserId).Returns(_member);

        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(new List<ChannelGroup>()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(() => TestDbSet.Create(new List<MessageMention>()).Object);

        _members.Setup(m => m.FindAsync(_workspaceId, _owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_owner, "Owner", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _guest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_guest, "Guest", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider, "Member", true));
    }

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space Channel(string name, SpaceKind kind = SpaceKind.ChannelPublic, Guid? workspaceId = null)
    {
        var space = Space.CreateChannel(_accountId, workspaceId ?? _workspaceId, kind, name, null, null, 1, _owner, Now);
        _spaces.Add(space);
        return space;
    }

    private void Join(Space space, Guid userId) => space.AddMember(userId, MembershipRole.Member, Now);

    /// <summary>Stores a user message at the sequence and moves the space's last sequence there, as the send does in the store.</summary>
    private void Store(Space space, long sequence, Guid author, string text, string? clientId = null)
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, sequence, author, text, clientId ?? $"c-{sequence}-{author}", null, Now);
        _messages.Add(message);
        SetLastSequence(space, sequence);
    }

    private void StoreSystem(Space space, long sequence, Guid actor)
    {
        _messages.Add(Message.CreateSystemMessage(_accountId, _workspaceId, space.Id, sequence, actor, "joined", Now));
        SetLastSequence(space, sequence);
    }

    /// <summary>The sequence is owned by the store (S1.2); the test sets the state the store would leave behind.</summary>
    private static void SetLastSequence(Space space, long value) =>
        typeof(Space).GetProperty(nameof(Space.LastSequence))!.GetSetMethod(nonPublic: true)!.Invoke(space, [value]);

    private static Membership MembershipOf(Space space, Guid userId) => space.GetActiveMembership(userId)!;

    private Task<Result<UnreadSummaryDto>> Summary() =>
        new GetUnreadSummaryQueryHandler(_db.Object, _context.Object, _members.Object, _clock.Object)
            .Handle(new GetUnreadSummaryQuery(_workspaceId), CancellationToken.None);

    private Task<Result<ReadBoundaryDto>> MarkRead(Guid spaceId, long upTo) =>
        new MarkSpaceReadCommandHandler(_db.Object, _context.Object, _members.Object, _store, _events.Object, _clock.Object)
            .Handle(new MarkSpaceReadCommand(_workspaceId, spaceId, upTo), CancellationToken.None);

    private Task<Result<NotificationSettingsDto>> SetLevel(Guid spaceId, string level, DateTimeOffset? until) =>
        new SetNotificationLevelCommandHandler(_db.Object, _context.Object, _members.Object, _clock.Object)
            .Handle(new SetNotificationLevelCommand(_workspaceId, spaceId, level, until), CancellationToken.None);

    // --- D01: unread summary ---

    [Fact]
    public async Task Summary_CountsOtherPeoplesLiveUserMessagesAfterTheBoundary_PerSpace_AndInTotal()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);
        Store(alpha, 1, _owner, "one");
        Store(alpha, 2, _member, "own");
        Store(alpha, 3, _owner, "three");
        StoreSystem(alpha, 4, _owner);
        Store(alpha, 5, _owner, "deleted later");
        _messages.Single(m => m.Sequence == 5).Delete(_owner, Now);
        Store(alpha, 6, _owner, "six");
        MembershipOf(alpha, _member).AdvanceReadSequence(1);

        var beta = Channel("beta");
        Join(beta, _member);
        for (var i = 1; i <= 4; i++) Store(beta, i, _owner, $"beta {i}");

        var gamma = Channel("gamma");
        Join(gamma, _member);
        Store(gamma, 1, _owner, "muted space");
        MembershipOf(gamma, _member).SetNotificationLevel(NotificationLevel.None, Now.AddHours(1), Now);

        var delta = Channel("delta");
        Join(delta, _member);

        var result = await Summary();

        result.Succeeded.Should().BeTrue();
        var summary = result.Data!;
        summary.Spaces.Select(s => s.Name).Should().Equal(new[] { "alpha", "beta", "delta", "gamma" }, "spaces are ordered by name");

        var a = summary.Spaces.Single(s => s.Name == "alpha");
        a.UnreadCount.Should().Be(2, "seq 3 and seq 6 are other people's live user messages after the boundary of 1");
        a.FirstUnreadSequence.Should().Be(3);
        a.ReadSequence.Should().Be(1);
        a.LastSequence.Should().Be(6);
        a.MentionCount.Should().Be(0, "mention facts do not exist before S2.4");

        var b = summary.Spaces.Single(s => s.Name == "beta");
        b.UnreadCount.Should().Be(4);
        b.FirstUnreadSequence.Should().Be(1);

        var d = summary.Spaces.Single(s => s.Name == "delta");
        d.UnreadCount.Should().Be(0);
        d.FirstUnreadSequence.Should().BeNull();

        var g = summary.Spaces.Single(s => s.Name == "gamma");
        g.UnreadCount.Should().Be(1, "a muted space still has its count");
        g.Muted.Should().BeTrue();
        g.NotificationLevel.Should().Be("none");

        summary.TotalUnread.Should().Be(6, "the total is alpha plus beta; the muted gamma is left out");
        summary.TotalMentions.Should().Be(0);
    }

    [Fact]
    public async Task Summary_ListsOnlyTheSpacesTheCallerIsAMemberOf()
    {
        AsUser(_member);
        var mine = Channel("mine");
        Join(mine, _member);
        Channel("not-mine");

        var summary = (await Summary()).Data!;

        summary.Spaces.Select(s => s.Name).Should().Equal("mine");
    }

    [Fact]
    public async Task Summary_OfAGuest_ListsOnlyTheSpacesTheGuestWasAddedTo()
    {
        AsUser(_guest);
        var added = Channel("added");
        Join(added, _guest);
        var notAdded = Channel("not-added");
        Store(notAdded, 1, _owner, "hidden");

        var summary = (await Summary()).Data!;

        summary.Spaces.Select(s => s.Name).Should().Equal("added");
    }

    [Fact]
    public async Task Summary_LeavesOutASpaceTheCallerLeftAndSpacesOfAnotherWorkspace()
    {
        AsUser(_member);
        var left = Channel("left");
        Join(left, _member);
        Store(left, 1, _owner, "before leaving");
        left.RemoveMember(_member, Now.AddMinutes(1));

        var foreign = Channel("foreign", workspaceId: _otherWorkspaceId);
        Join(foreign, _member);

        var summary = (await Summary()).Data!;

        summary.Spaces.Should().BeEmpty();
        summary.TotalUnread.Should().Be(0);
    }

    [Fact]
    public async Task Summary_ForAWorkspaceNonMember_IsNotFound()
    {
        AsUser(_outsider);
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkspaceMemberFact?)null);

        var act = () => Summary();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // --- D02: mark a space read ---

    [Fact]
    public async Task MarkSpaceRead_AnnouncesTheNewCountOnThePersonsOwnStream()
    {
        // UC-CHT-C09: the person's other devices learn the space's new count from their per-user stream.
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);
        for (var i = 1; i <= 3; i++) Store(alpha, i, _owner, $"m{i}");
        var added = new List<IIntegrationEvent>();
        _events.Setup(e => e.Add(It.IsAny<IIntegrationEvent>())).Callback<IIntegrationEvent>(added.Add);

        await MarkRead(alpha.Id, 3);

        var fact = added.OfType<RealtimeResourceChangedV1>().Should().ContainSingle().Which;
        fact.TopicNamespace.Should().Be("user");
        fact.ResourceId.Should().Be(_member);
        fact.ChangeKind.Should().Be("chat.unread.changed");
    }

    [Fact]
    public async Task MarkSpaceRead_MovesTheBoundaryForward_AndClampsItToTheLastSequence()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);
        for (var i = 1; i <= 6; i++) Store(alpha, i, _owner, $"m{i}");

        var result = await MarkRead(alpha.Id, 99);

        result.Succeeded.Should().BeTrue();
        result.Data!.ReadSequence.Should().Be(6, "a request above the last sequence reads the space to its end");
        result.Data.LastSequence.Should().Be(6);
    }

    [Fact]
    public async Task MarkSpaceRead_IsMonotonic_ALowerRequestChangesNothing()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);
        for (var i = 1; i <= 6; i++) Store(alpha, i, _owner, $"m{i}");

        await MarkRead(alpha.Id, 4);
        var lower = await MarkRead(alpha.Id, 2);

        lower.Data!.ReadSequence.Should().Be(4, "the boundary never moves backwards");
    }

    [Fact]
    public async Task MarkSpaceRead_IsIdempotent()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);
        Store(alpha, 1, _owner, "one");

        var first = await MarkRead(alpha.Id, 1);
        var second = await MarkRead(alpha.Id, 1);

        second.Data!.ReadSequence.Should().Be(first.Data!.ReadSequence);
    }

    [Fact]
    public async Task MarkSpaceRead_ForAPrivateSpaceTheCallerIsNotIn_IsNotFound()
    {
        AsUser(_member);
        var secret = Channel("secret", SpaceKind.ChannelPrivate);
        Join(secret, _owner);
        Store(secret, 1, _owner, "private");

        var act = () => MarkRead(secret.Id, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MarkSpaceRead_ForAPublicChannelTheCallerIsNotIn_IsTheSameNotFound()
    {
        AsUser(_member);
        var open = Channel("open");
        Store(open, 1, _owner, "public");

        var act = () => MarkRead(open.Id, 1);

        await act.Should().ThrowAsync<NotFoundException>("a non-member has no read state in a space");
    }

    [Fact]
    public async Task MarkSpaceRead_ForAChannelOfAnotherWorkspace_IsNotFound()
    {
        AsUser(_member);
        var foreign = Channel("foreign", workspaceId: _otherWorkspaceId);
        Join(foreign, _member);

        var act = () => MarkRead(foreign.Id, 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MarkSpaceRead_ForAMissingSpace_IsTheSameNotFound()
    {
        AsUser(_member);

        var act = () => MarkRead(Guid.NewGuid(), 1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void MarkSpaceRead_RefusesANegativeSequence()
    {
        var validator = new MarkSpaceReadCommandValidator();

        validator.TestValidate(new MarkSpaceReadCommand(_workspaceId, Guid.NewGuid(), -1))
            .ShouldHaveValidationErrorFor(x => x.UpToSequence);
    }

    // --- D07: mark all read ---

    [Fact]
    public async Task MarkAllRead_MovesEveryLiveSpaceOfTheCaller_ToItsLastSequence()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);
        for (var i = 1; i <= 3; i++) Store(alpha, i, _owner, $"a{i}");
        var beta = Channel("beta");
        Join(beta, _member);
        Store(beta, 1, _owner, "b1");
        var notMine = Channel("not-mine");
        Store(notMine, 1, _owner, "untouched");

        var result = await new MarkAllReadCommandHandler(_db.Object, _context.Object, _members.Object, _store, _clock.Object, _events.Object)
            .Handle(new MarkAllReadCommand(_workspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalUnread.Should().Be(0);
        result.Data.Spaces.Should().OnlyContain(s => s.UnreadCount == 0);
        MembershipOf(alpha, _member).ReadSequence.Should().Be(3);
        MembershipOf(beta, _member).ReadSequence.Should().Be(1);
    }

    [Fact]
    public async Task MarkAllRead_LeavesAMessageSentAfterTheLastSequenceUnread()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);
        Store(alpha, 1, _owner, "before");
        await new MarkAllReadCommandHandler(_db.Object, _context.Object, _members.Object, _store, _clock.Object, _events.Object)
            .Handle(new MarkAllReadCommand(_workspaceId), CancellationToken.None);

        Store(alpha, 2, _owner, "arrived after the call");

        var summary = (await Summary()).Data!;

        summary.Spaces.Single().UnreadCount.Should().Be(1);
        summary.Spaces.Single().FirstUnreadSequence.Should().Be(2);
    }

    // --- D05, D06: mute and notification level ---

    [Fact]
    public async Task SetNotificationLevel_MutesTheSpace_AndItLeavesTheTotals()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);
        Store(alpha, 1, _owner, "x");
        var until = Now.AddHours(1);

        var result = await SetLevel(alpha.Id, "none", until);

        result.Data!.Muted.Should().BeTrue();
        result.Data.NotificationLevel.Should().Be("none");
        result.Data.MutedUntil.Should().Be(until);

        var summary = (await Summary()).Data!;
        summary.Spaces.Single().Muted.Should().BeTrue();
        summary.TotalUnread.Should().Be(0);
    }

    [Fact]
    public async Task SetNotificationLevel_WithAPastUntil_IsANoOp()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);

        var result = await SetLevel(alpha.Id, "none", Now.AddMinutes(-1));

        result.Data!.NotificationLevel.Should().Be("all");
        result.Data.Muted.Should().BeFalse();
        result.Data.MutedUntil.Should().BeNull();
    }

    [Fact]
    public async Task SetNotificationLevel_ToMentions_StoresTheLevel()
    {
        AsUser(_member);
        var alpha = Channel("alpha");
        Join(alpha, _member);

        var result = await SetLevel(alpha.Id, "mentions", null);

        result.Data!.NotificationLevel.Should().Be("mentions");
        result.Data.Muted.Should().BeFalse();
        MembershipOf(alpha, _member).NotificationLevel.Should().Be(NotificationLevel.Mentions);
    }

    [Fact]
    public async Task SetNotificationLevel_ForANonMemberOfAPublicChannel_IsNotFound()
    {
        AsUser(_member);
        var open = Channel("open");

        var act = () => SetLevel(open.Id, "mentions", null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void SetNotificationLevel_RefusesAnUntilWithALevelOtherThanNone()
    {
        var validator = new SetNotificationLevelCommandValidator();

        validator.TestValidate(new SetNotificationLevelCommand(_workspaceId, Guid.NewGuid(), "mentions", Now.AddHours(1)))
            .Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void SetNotificationLevel_AcceptsAMuteWithAnUntil()
    {
        var validator = new SetNotificationLevelCommandValidator();

        validator.TestValidate(new SetNotificationLevelCommand(_workspaceId, Guid.NewGuid(), "none", Now.AddHours(1)))
            .IsValid.Should().BeTrue();
    }

    /// <summary>The store's rule in memory: the boundary moves to GREATEST(current, target), bounded by what the caller holds.</summary>
    private sealed class FakeChatReadStateStore(List<Space> spaces) : IChatReadStateStore
    {
        public Task<bool> AdvanceAsync(Guid workspaceId, Guid spaceId, Guid userId, long target, CancellationToken cancellationToken)
        {
            var space = spaces.SingleOrDefault(s => s.Id == spaceId && s.WorkspaceId == workspaceId);
            var membership = space?.GetActiveMembership(userId);
            if (membership is null) return Task.FromResult(false);

            membership.AdvanceReadSequence(target);
            return Task.FromResult(true);
        }

        public Task<int> AdvanceAllAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
        {
            var touched = 0;
            foreach (var space in spaces.Where(s => s.WorkspaceId == workspaceId && !s.IsDeleted))
            {
                var membership = space.GetActiveMembership(userId);
                if (membership is null) continue;
                membership.AdvanceReadSequence(space.LastSequence);
                touched++;
            }
            return Task.FromResult(touched);
        }
    }
}
