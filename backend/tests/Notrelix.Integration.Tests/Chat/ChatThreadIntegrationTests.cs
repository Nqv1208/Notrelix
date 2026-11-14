using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Chat.Threads.Commands.FollowThread;
using Notrelix.Application.Features.Chat.Threads.Commands.MarkThreadRead;
using Notrelix.Application.Features.Chat.Threads.Commands.ReplyInThread;
using Notrelix.Application.Features.Chat.Threads.Queries.ListFollowedThreads;
using Notrelix.Application.Features.Chat.Threads.Queries.ListThreadReplies;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-F01, F03, F04, F08 against PostgreSQL. Each reply runs in its own context and transaction, the way the pipeline runs
/// a write request: the space row lock must serialize the thread sequence and the space sequence, so concurrent replies get a
/// gap-free run of thread sequences and a racing duplicate clientMessageId stores exactly one reply. The read pointer store
/// proves the GREATEST rule in SQL. The follow list query is run through EF against the real joins. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatThreadIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private const int Racers = 15;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private Guid _spaceId;
    private Guid _rootId;

    public ChatThreadIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "threads", null, null, 1, _member, Now);
        _spaceId = channel.Id;

        // The root takes sequence 1 from the same allocator a send uses, so the space's last sequence matches it.
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ChatSpaces.Add(channel);
        await seed.SaveChangesAsync();
        var sequence = await new ChatSpaceSequenceAllocator(seed).NextAsync(_spaceId, CancellationToken.None);
        var root = Message.CreateUserMessage(_accountId, _workspaceId, _spaceId, sequence, _member, "root", "client-root", null, Now);
        _rootId = root.Id;
        seed.ChatMessages.Add(root);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private Mock<ICurrentRequestContext> RequestAs(Guid userId)
    {
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(userId);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        return context;
    }

    private Mock<IWorkspaceMemberFacts> ActiveMembers(Guid userId)
    {
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, "Member", true));
        return members;
    }

    /// <summary>One reply, in its own transaction, the way the pipeline runs a write request.</summary>
    private async Task<Result<Notrelix.Application.Features.Chat.Messages.DTOs.SendMessageResultDto>> ReplyInOwnTransactionAsync(string content, string clientMessageId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await using var transaction = await context.Database.BeginTransactionAsync();

        var handler = new ReplyInThreadCommandHandler(
            context,
            RequestAs(_member).Object,
            ActiveMembers(_member).Object,
            new ChatSpaceSequenceAllocator(context),
            new FixedClock(Now),
            Mock.Of<Notrelix.Application.Features.Chat.Mentions.Services.IBroadcastAuthorizer>(),
            new InMemoryChatMessageAttachments(), Moq.Mock.Of<Notrelix.Application.Features.Chat.RateLimits.IChatRateLimiter>());

        var result = await handler.Handle(
            new ReplyInThreadCommand(_workspaceId, _rootId, content, clientMessageId, AlsoSendToChannel: false),
            CancellationToken.None);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    [Fact]
    public async Task TwentyConcurrentReplies_GetAGapFreeRunOfThreadSequences_AndTheCountMatches()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, Racers)
            .Select(i => ReplyInOwnTransactionAsync($"reply {i}", $"reply-{i}")));

        results.Should().OnlyContain(r => r.Succeeded && r.Data!.Created);
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var threadSequences = await probe.ChatMessages.IgnoreQueryFilters()
            .Where(m => m.ThreadRootId == _rootId)
            .Select(m => m.ThreadSequence!.Value)
            .ToListAsync();
        threadSequences.Order().Should().Equal(Enumerable.Range(1, Racers).Select(i => (long)i),
            "the space lock serializes the thread sequence, so no number is skipped or repeated");

        var thread = await probe.ChatThreads.IgnoreQueryFilters().SingleAsync(t => t.RootMessageId == _rootId);
        thread.ReplyCount.Should().Be(Racers);

        var spaceSequences = await probe.ChatMessages.IgnoreQueryFilters()
            .Where(m => m.SpaceId == _spaceId)
            .Select(m => m.Sequence)
            .ToListAsync();
        spaceSequences.Order().Should().Equal(Enumerable.Range(1, Racers + 1).Select(i => (long)i),
            "the replies share the space sequence with the root, gap-free");
    }

    [Fact]
    public async Task RacingDuplicateClientMessageId_StoresOneReply()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => ReplyInOwnTransactionAsync("same", "reply-dup")));

        results.Should().OnlyContain(r => r.Succeeded);
        results.Count(r => r.Data!.Created).Should().Be(1, "exactly one racer creates the reply; the others get it back");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatMessages.IgnoreQueryFilters().CountAsync(m => m.ThreadRootId == _rootId)).Should().Be(1);
        (await probe.ChatThreads.IgnoreQueryFilters().SingleAsync(t => t.RootMessageId == _rootId)).ReplyCount.Should().Be(1);
    }

    [Fact]
    public async Task TheReadPointer_IsMonotonic_InSql()
    {
        // The member replies three times: the replier follows the thread and their pointer is at their last reply, 3.
        await ReplyInOwnTransactionAsync("first", "reply-a");
        await ReplyInOwnTransactionAsync("second", "reply-b");
        await ReplyInOwnTransactionAsync("third", "reply-c");

        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            var store = new ChatThreadReadStore(context);

            (await store.AdvanceAsync(_workspaceId, _rootId, _member, 2, CancellationToken.None)).Should().BeTrue();
            (await store.AdvanceAsync(_workspaceId, _rootId, _member, 1, CancellationToken.None)).Should().BeTrue("the row exists; the value does not move back");
            (await store.AdvanceAsync(_workspaceId, _rootId, Guid.NewGuid(), 9, CancellationToken.None)).Should().BeFalse("a person who does not follow has no pointer");
            await context.SaveChangesAsync();
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatThreadFollowers.IgnoreQueryFilters().SingleAsync()).ReadReplySequence.Should().Be(3,
            "GREATEST keeps the pointer at 3 when a lower value arrives");
    }

    [Fact]
    public async Task ListingThreadsIFollow_ReturnsUnreadCountsThroughRealJoins()
    {
        await ReplyInOwnTransactionAsync("first", "reply-x");
        await ReplyInOwnTransactionAsync("second", "reply-y");

        await using var context = _db.CreateOwnerContext(SystemTenant());
        // The member replied twice and is the root's author, so they follow the thread with their pointer at their last reply.
        var listed = await new ListFollowedThreadsQueryHandler(context, RequestAs(_member).Object, ActiveMembers(_member).Object, new FixedClock(Now), new InMemoryChatMessageAttachments())
            .Handle(new ListFollowedThreadsQuery(_workspaceId, null, 50), CancellationToken.None);

        listed.Succeeded.Should().BeTrue();
        listed.Data!.Items.Should().ContainSingle();
        var item = listed.Data.Items.Single();
        item.Root.Id.Should().Be(_rootId);
        item.Thread.ReplyCount.Should().Be(2);
        item.Thread.Following.Should().BeTrue();
        item.Thread.UnreadReplies.Should().Be(0, "the replier has read every reply they posted");
    }

    [Fact]
    public async Task TheHistoryRoot_CarriesTheStoredReplyCountThroughOneBatchedThreadQuery()
    {
        for (var i = 0; i < 3; i++) await ReplyInOwnTransactionAsync($"reply {i}", $"count-{i}");

        await using var context = _db.CreateOwnerContext(SystemTenant());
        var handler = new ListMessagesQueryHandler(context, RequestAs(_member).Object, ActiveMembers(_member).Object, new InMemoryChatMessageAttachments());

        var page = await handler.Handle(new ListMessagesQuery(_workspaceId, _spaceId, null, null, null, 50), CancellationToken.None);

        var root = page.Data!.Items.Single(m => m.Id == _rootId);
        root.ThreadReplyCount.Should().Be(3);
        root.ThreadLastReplyAt.Should().Be(Now);
    }

    [Fact]
    public async Task RepliesPage_ReadsInThreadOrderThroughEf()
    {
        for (var i = 0; i < 3; i++) await ReplyInOwnTransactionAsync($"reply {i}", $"page-{i}");

        await using var context = _db.CreateOwnerContext(SystemTenant());
        var handler = new ListThreadRepliesQueryHandler(context, RequestAs(_member).Object, ActiveMembers(_member).Object, new FixedClock(Now), new InMemoryChatMessageAttachments());

        var page = await handler.Handle(new ListThreadRepliesQuery(_workspaceId, _rootId, After: 1, Limit: 1), CancellationToken.None);

        page.Data!.Items.Should().ContainSingle().Which.ThreadSequence.Should().Be(2);
        page.Data.HasNewer.Should().BeTrue();
        page.Data.Root.Id.Should().Be(_rootId);
    }

    [Fact]
    public async Task FollowAndMarkRead_RoundTripOnTheRealDatabase()
    {
        await ReplyInOwnTransactionAsync("first", "rt-1");
        await ReplyInOwnTransactionAsync("second", "rt-2");

        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            var follow = await new FollowThreadCommandHandler(context, RequestAs(_member).Object, ActiveMembers(_member).Object)
                .Handle(new FollowThreadCommand(_workspaceId, _rootId, true, Muted: true), CancellationToken.None);
            follow.Data!.Muted.Should().BeTrue();
            await context.SaveChangesAsync();
        }

        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            // The pointer is already at the last reply, so asking for an earlier one leaves nothing unread and moves nothing.
            var read = await new MarkThreadReadCommandHandler(context, RequestAs(_member).Object, ActiveMembers(_member).Object, new ChatThreadReadStore(context))
                .Handle(new MarkThreadReadCommand(_workspaceId, _rootId, 1), CancellationToken.None);
            read.Data!.UnreadReplies.Should().Be(0);
            await context.SaveChangesAsync();
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        var follower = await probe.ChatThreadFollowers.IgnoreQueryFilters().SingleAsync();
        follower.Muted.Should().BeTrue();
        follower.ReadReplySequence.Should().Be(2);
    }
}
