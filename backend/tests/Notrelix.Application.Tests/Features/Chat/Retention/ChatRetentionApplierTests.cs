using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.ReadState.Services;
using Notrelix.Application.Features.Chat.Retention.Services;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Messages.Events;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Chat.Spaces.Events;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Application.Tests.Features.Collaboration;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Retention;

/// <summary>
/// UC-CHT-H05: the retention job's batches at the level of the applier, over in-memory sets. The advisory lock, the transaction and the
/// commit belong to the Infrastructure runner; what a batch selects, removes and reports is decided here.
/// </summary>
public class ChatRetentionApplierTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<MessageThread> _threads = [];
    private readonly List<ThreadFollower> _followers = [];
    private readonly List<Pin> _pins = [];
    private readonly List<Reaction> _reactions = [];
    private readonly List<MessageMention> _mentions = [];
    private readonly List<RetentionPolicy> _policies = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly InMemoryChatMessageAttachments _files = new();
    private readonly FakeSystemAudit _audit = new();
    private readonly Dictionary<Guid, long> _lastSequence = [];

    public ChatRetentionApplierTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatThreads).Returns(() => TestDbSet.Create(_threads).Object);
        _db.Setup(d => d.ChatThreadFollowers).Returns(() => TestDbSet.Create(_followers).Object);
        _db.Setup(d => d.ChatPins).Returns(() => TestDbSet.Create(_pins).Object);
        _db.Setup(d => d.ChatReactions).Returns(() => TestDbSet.Create(_reactions).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(() => TestDbSet.Create(_mentions).Object);
        _db.Setup(d => d.ChatRetentionPolicies).Returns(() => TestDbSet.Create(_policies).Object);
    }

    private ChatRetentionApplier Applier() => new(_db.Object, _files, _audit, _clock.Object);

    private RetentionPolicy Rule(int days, RetentionMode mode, Guid? workspace = null)
    {
        var policy = RetentionPolicy.CreateDefault(_accountId, workspace ?? _workspaceId, Now.AddDays(-200));
        policy.Change(days, mode, _owner, Now.AddDays(-150));
        _policies.Add(policy);
        return policy;
    }

    private RetentionTarget Target(int days, RetentionMode mode) => new(_accountId, _workspaceId, days, mode, _owner);

    private Space Channel(string name = "general", SpaceKind kind = SpaceKind.ChannelPublic)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, name, null, null, 1, _owner, Now.AddDays(-300));
        space.AddMember(_alice, MembershipRole.Member, Now.AddDays(-300));
        space.AddMember(_bob, MembershipRole.Member, Now.AddDays(-300));
        _spaces.Add(space);
        return space;
    }

    private Message Text(Space space, Guid author, int ageDays, string content = "words")
    {
        var sequence = _lastSequence.GetValueOrDefault(space.Id) + 1;
        _lastSequence[space.Id] = sequence;
        var message = Message.CreateUserMessage(
            _accountId, _workspaceId, space.Id, sequence, author, content, $"c-{Guid.NewGuid():N}", null, Now.AddDays(-ageDays));
        _messages.Add(message);
        return message;
    }

    private Message Reply(Space space, Message root, Guid author, int ageDays)
    {
        var thread = _threads.FirstOrDefault(t => t.RootMessageId == root.Id);
        if (thread is null)
        {
            thread = MessageThread.Open(_accountId, _workspaceId, space.Id, root.Id, Now.AddDays(-ageDays));
            _threads.Add(thread);
            _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, root.Id, author, false));
        }

        var sequence = _lastSequence.GetValueOrDefault(space.Id) + 1;
        _lastSequence[space.Id] = sequence;
        var threadSequence = thread.RecordReply(Now.AddDays(-ageDays));
        var reply = Message.CreateThreadReply(
            _accountId, _workspaceId, space.Id, sequence, root.Id, threadSequence, author, "a reply", $"r-{Guid.NewGuid():N}", false, Now.AddDays(-ageDays));
        _messages.Add(reply);
        return reply;
    }

    private async Task<int> Drain(RetentionTarget target, Guid spaceId)
    {
        var total = 0;
        for (var i = 0; i < 100; i++)
        {
            var result = await Applier().ApplyBatchAsync(target, spaceId, CancellationToken.None);
            if (result.IsEmpty) break;
            total += result.Messages;
        }
        return total;
    }

    // ---- targets and selection ------------------------------------------------------------------

    [Fact]
    public async Task OnlyAWorkspaceWithAWindowIsATarget()
    {
        Rule(90, RetentionMode.Purge);
        var offPolicy = RetentionPolicy.CreateDefault(_accountId, Guid.NewGuid(), Now);
        _policies.Add(offPolicy);

        var targets = await Applier().ListTargetsAsync(CancellationToken.None);

        targets.Should().ContainSingle();
        targets[0].WorkspaceId.Should().Be(_workspaceId);
        targets[0].RetentionDays.Should().Be(90);
        targets[0].Mode.Should().Be(RetentionMode.Purge);
        targets[0].ConfiguredBy.Should().Be(_owner);
    }

    [Fact]
    public async Task ASpaceIsListedOnlyWhenItHoldsAnExpiredMessage_AndADeletedSpaceIsHeld()
    {
        var old = Channel("old");
        var fresh = Channel("fresh");
        var deleted = Channel("gone");
        Text(old, _alice, ageDays: 100);
        Text(fresh, _alice, ageDays: 10);
        Text(deleted, _alice, ageDays: 100);
        deleted.Delete(_owner, Now.AddDays(-5));

        var listed = await Applier().ListSpacesWithExpiredAsync(Target(90, RetentionMode.Tombstone), CancellationToken.None);

        listed.Should().ContainSingle().Which.Should().Be(old.Id);
    }

    // ---- tombstone mode -------------------------------------------------------------------------

    [Fact]
    public async Task TombstoneMode_TurnsOnlyExpiredUserMessagesIntoTombstones_KeepingAuthorTimeAndSequence()
    {
        var space = Channel();
        var expired = Text(space, _alice, ageDays: 100, "private words");
        var recent = Text(space, _bob, ageDays: 10);
        var system = Message.CreateSystemMessage(_accountId, _workspaceId, space.Id, 99, _owner, "alice joined", Now.AddDays(-200));
        _messages.Add(system);
        var expiredSequence = expired.Sequence;
        var versionBefore = expired.Version;

        var result = await Applier().ApplyBatchAsync(Target(90, RetentionMode.Tombstone), space.Id, CancellationToken.None);

        result.Messages.Should().Be(1);
        result.Threads.Should().Be(0);
        expired.IsDeleted.Should().BeTrue();
        expired.Content.Should().BeNull();
        expired.AuthorId.Should().Be(_alice);
        expired.Sequence.Should().Be(expiredSequence);
        expired.Version.Should().Be(versionBefore + 1);
        recent.IsDeleted.Should().BeFalse();
        recent.Content.Should().Be("words");
        system.IsDeleted.Should().BeFalse("a system message holds no one's words");
        _messages.Should().Contain(expired, "a tombstone is kept");
    }

    [Fact]
    public async Task TombstoneMode_RemovesThePinReactionsMentionsAndFilesOfTheMessage()
    {
        var space = Channel();
        var expired = Text(space, _alice, ageDays: 100);
        var recent = Text(space, _alice, ageDays: 5);
        _pins.Add(Pin.Create(_accountId, _workspaceId, space.Id, expired.Id, _owner, Now.AddDays(-99)));
        _pins.Add(Pin.Create(_accountId, _workspaceId, space.Id, recent.Id, _owner, Now.AddDays(-4)));
        _reactions.Add(Reaction.Create(_accountId, _workspaceId, space.Id, expired.Id, _bob, "👍", Now.AddDays(-99)));
        _reactions.Add(Reaction.Create(_accountId, _workspaceId, space.Id, recent.Id, _bob, "👍", Now.AddDays(-4)));
        _mentions.Add(MessageMention.Create(_accountId, _workspaceId, space.Id, expired.Id, new MentionSpan(MentionKind.User, 0, 2, _bob)));
        _files.Seed(expired.Id);
        _files.Seed(recent.Id);

        await Applier().ApplyBatchAsync(Target(90, RetentionMode.Tombstone), space.Id, CancellationToken.None);

        _pins.Should().ContainSingle().Which.MessageId.Should().Be(recent.Id);
        _reactions.Should().ContainSingle().Which.MessageId.Should().Be(recent.Id);
        _mentions.Should().BeEmpty();
        _files.Files.Should().ContainSingle().Which.MessageId.Should().Be(recent.Id, "the files of an expired message are removed so the object job purges them");
    }

    [Fact]
    public async Task TombstoneMode_DoesNotSelectATombstoneAgain_AndASecondRunChangesNothing()
    {
        var space = Channel();
        var expired = Text(space, _alice, ageDays: 100);
        var target = Target(90, RetentionMode.Tombstone);
        await Applier().ApplyBatchAsync(target, space.Id, CancellationToken.None);
        var version = expired.Version;
        _audit.Retention.Clear();

        var second = await Applier().ApplyBatchAsync(target, space.Id, CancellationToken.None);

        second.IsEmpty.Should().BeTrue();
        expired.Version.Should().Be(version);
        _audit.Retention.Should().BeEmpty("an empty batch writes no fact");
        (await Applier().ListSpacesWithExpiredAsync(target, CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task TombstoneMode_ThreadRepliesExpireOneByOne_AndTheThreadCountStays()
    {
        var space = Channel();
        var root = Text(space, _alice, ageDays: 100);
        var oldReply = Reply(space, root, _bob, ageDays: 95);
        var newReply = Reply(space, root, _bob, ageDays: 2);

        await Drain(Target(90, RetentionMode.Tombstone), space.Id);

        root.IsDeleted.Should().BeTrue();
        oldReply.IsDeleted.Should().BeTrue();
        newReply.IsDeleted.Should().BeFalse();
        _threads.Should().ContainSingle().Which.ReplyCount.Should().Be(2, "a tombstone keeps its place, as a deleted reply does");
        _followers.Should().ContainSingle();
    }

    // ---- purge mode -----------------------------------------------------------------------------

    [Fact]
    public async Task PurgeMode_DeletesTheExpiredRowsForGood_ButNotTheRecentOnes_AndKeepsTheSpaceSequence()
    {
        var space = Channel();
        var expired = Text(space, _alice, ageDays: 100);
        var alreadyTombstone = Text(space, _alice, ageDays: 120);
        alreadyTombstone.Delete(_alice, Now.AddDays(-119));
        var recent = Text(space, _bob, ageDays: 10);
        var lastSequence = space.LastSequence;

        var result = await Applier().ApplyBatchAsync(Target(90, RetentionMode.Purge), space.Id, CancellationToken.None);

        result.Messages.Should().Be(2);
        _messages.Should().ContainSingle().Which.Should().BeSameAs(recent);
        space.LastSequence.Should().Be(lastSequence, "a purged sequence is never reused");
    }

    [Fact]
    public async Task PurgeMode_ARootWithALiveReplyStays_AndAThreadThatIsAllOldGoesAsAWhole()
    {
        var space = Channel();
        var liveRoot = Text(space, _alice, ageDays: 100);
        Reply(space, liveRoot, _bob, ageDays: 95);
        var liveReply = Reply(space, liveRoot, _bob, ageDays: 3);
        var deadRoot = Text(space, _alice, ageDays: 200);
        Reply(space, deadRoot, _bob, ageDays: 150);
        Reply(space, deadRoot, _bob, ageDays: 120);

        var result = await Applier().ApplyBatchAsync(Target(90, RetentionMode.Purge), space.Id, CancellationToken.None);

        result.Messages.Should().Be(3, "the dead root and its two replies");
        result.Threads.Should().Be(1);
        _messages.Select(m => m.Id).Should().BeEquivalentTo(
            _messages.Where(m => m.Id == liveRoot.Id || m.ThreadRootId == liveRoot.Id).Select(m => m.Id));
        _messages.Should().Contain(liveRoot).And.Contain(liveReply);
        _threads.Should().ContainSingle().Which.RootMessageId.Should().Be(liveRoot.Id);
        _followers.Should().ContainSingle().Which.RootMessageId.Should().Be(liveRoot.Id);
    }

    [Fact]
    public async Task PurgeMode_RemovesWhatHangsOnAPurgedMessage()
    {
        var space = Channel();
        var expired = Text(space, _alice, ageDays: 100);
        _pins.Add(Pin.Create(_accountId, _workspaceId, space.Id, expired.Id, _owner, Now.AddDays(-99)));
        _reactions.Add(Reaction.Create(_accountId, _workspaceId, space.Id, expired.Id, _bob, "👍", Now.AddDays(-99)));
        _files.Seed(expired.Id);

        await Applier().ApplyBatchAsync(Target(90, RetentionMode.Purge), space.Id, CancellationToken.None);

        _pins.Should().BeEmpty();
        _reactions.Should().BeEmpty();
        _files.Files.Should().BeEmpty();
    }

    // ---- batches, holds, audit, realtime --------------------------------------------------------

    [Fact]
    public async Task ABatchHasABoundedSize_AndTheJobResumesWhereItStopped()
    {
        var space = Channel();
        for (var i = 0; i < ChatLimits.RetentionBatchSize + 30; i++)
            Text(space, _alice, ageDays: 100 + i % 5);
        var target = Target(90, RetentionMode.Tombstone);

        var first = await Applier().ApplyBatchAsync(target, space.Id, CancellationToken.None);
        first.Messages.Should().Be(ChatLimits.RetentionBatchSize);
        _messages.Count(m => m.IsDeleted).Should().Be(ChatLimits.RetentionBatchSize);

        // A pass that stopped here (a crash between batches) is resumed by the next one: it selects what is left.
        var second = await Applier().ApplyBatchAsync(target, space.Id, CancellationToken.None);
        second.Messages.Should().Be(30);
        _messages.Should().OnlyContain(m => m.IsDeleted);
    }

    [Fact]
    public async Task ADeletedSpaceIsUnderTheRecoveryHold_AndNothingIsRemovedFromIt()
    {
        var space = Channel();
        var expired = Text(space, _alice, ageDays: 100);
        space.Delete(_owner, Now.AddDays(-3));

        var result = await Applier().ApplyBatchAsync(Target(90, RetentionMode.Purge), space.Id, CancellationToken.None);

        result.IsEmpty.Should().BeTrue();
        _messages.Should().Contain(expired);
        _audit.Retention.Should().BeEmpty();
    }

    [Fact]
    public async Task AnotherWorkspacesSpaceIsNeverTouched()
    {
        var space = Channel();
        Text(space, _alice, ageDays: 100);
        var other = new RetentionTarget(_accountId, Guid.NewGuid(), 90, RetentionMode.Purge, _owner);

        var result = await Applier().ApplyBatchAsync(other, space.Id, CancellationToken.None);

        result.IsEmpty.Should().BeTrue();
        _messages.Should().ContainSingle();
    }

    [Fact]
    public async Task OneAuditFactIsWrittenPerBatch_WithCountsTheSpaceAndTheRule_AndNeverContent()
    {
        var space = Channel();
        for (var i = 0; i < ChatLimits.RetentionBatchSize + 5; i++)
            Text(space, _alice, ageDays: 100, "secret content");
        var target = Target(90, RetentionMode.Tombstone);

        await Drain(target, space.Id);

        _audit.Retention.Should().HaveCount(2, "two batches, two facts");
        var first = _audit.Retention[0];
        first.WorkspaceId.Should().Be(_workspaceId);
        first.SpaceId.Should().Be(space.Id);
        first.Mode.Should().Be("tombstone");
        first.RetentionDays.Should().Be(90);
        first.Cutoff.Should().Be(Now.AddDays(-90));
        first.Messages.Should().Be(ChatLimits.RetentionBatchSize);
        _audit.Retention[1].Messages.Should().Be(5);
        typeof(RetentionBatchFact).GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Content", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ABatchAnnouncesItselfOnceOnTheSpace_NotOncePerMessage()
    {
        var space = Channel();
        for (var i = 0; i < 20; i++)
            Text(space, _alice, ageDays: 100);

        await Applier().ApplyBatchAsync(Target(90, RetentionMode.Tombstone), space.Id, CancellationToken.None);

        space.DomainEvents.OfType<ChannelRetentionAppliedDomainEvent>().Should().ContainSingle();
        _messages.SelectMany(m => m.DomainEvents).OfType<MessageDeletedDomainEvent>().Should().BeEmpty();
    }

    [Fact]
    public async Task ABatchThatFindsNothingWritesNoFactAndNoAnnouncement()
    {
        var space = Channel();
        Text(space, _alice, ageDays: 10);

        var result = await Applier().ApplyBatchAsync(Target(90, RetentionMode.Purge), space.Id, CancellationToken.None);

        result.IsEmpty.Should().BeTrue();
        _audit.Retention.Should().BeEmpty();
        space.DomainEvents.OfType<ChannelRetentionAppliedDomainEvent>().Should().BeEmpty();
    }

    // ---- read state -----------------------------------------------------------------------------

    [Theory]
    [InlineData(RetentionMode.Tombstone)]
    [InlineData(RetentionMode.Purge)]
    public async Task UnreadCountsStayRight_ExpiredMessagesStopCounting_AndTheBoundaryNeverMovesBackwards(RetentionMode mode)
    {
        var space = Channel();
        Text(space, _bob, ageDays: 100);
        Text(space, _bob, ageDays: 99);
        Text(space, _bob, ageDays: 5);
        Text(space, _bob, ageDays: 2);
        typeof(Space).GetProperty(nameof(Space.LastSequence))!.SetValue(space, _lastSequence[space.Id]);
        var before = await UnreadSummaryBuilder.BuildAsync(_db.Object, _workspaceId, _alice, Now, CancellationToken.None);
        before.Spaces.Single().UnreadCount.Should().Be(4);

        await Drain(Target(90, mode), space.Id);

        var after = await UnreadSummaryBuilder.BuildAsync(_db.Object, _workspaceId, _alice, Now, CancellationToken.None);
        after.Spaces.Single().UnreadCount.Should().Be(2, "only the two recent messages are still there to read");
        after.Spaces.Single().ReadSequence.Should().Be(0);
        after.Spaces.Single().LastSequence.Should().Be(space.LastSequence, "the sequence counter is untouched, so the next message takes the next number");
    }

    private sealed class FakeSystemAudit : IChatSystemAudit
    {
        public List<RetentionBatchFact> Retention { get; } = [];
        public List<ExportCompletedFact> Exports { get; } = [];

        public Task RecordRetentionBatchAsync(RetentionBatchFact fact, CancellationToken cancellationToken)
        {
            Retention.Add(fact);
            return Task.CompletedTask;
        }

        public Task RecordExportCompletedAsync(ExportCompletedFact fact, CancellationToken cancellationToken)
        {
            Exports.Add(fact);
            return Task.CompletedTask;
        }
    }
}
