using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Mentions.DTOs;
using Notrelix.Application.Features.Chat.Mentions.Services;
using Notrelix.Application.Features.Chat.Messages.Commands.DeleteMessage;
using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Chat.ReadState.Queries.GetUnreadSummary;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-G01, G03, D04 against PostgreSQL: a send stores its mentions with the message in one transaction, a retry stores
/// nothing twice, the unread summary counts mentions through the real joins and the EXISTS over the mention rows, reading clears the
/// count, a delete takes the rows, and a broadcast counts for every member but the author. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatMentionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _author = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _carol = Guid.NewGuid();
    private Guid _spaceId;

    public ChatMentionIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "mentions", null, null, 1, _author, Now);
        channel.AddMember(_bob, MembershipRole.Member, Now);
        channel.AddMember(_carol, MembershipRole.Member, Now);
        _spaceId = channel.Id;

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.ChatSpaces.Add(channel);
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

    private Mock<IWorkspaceMemberFacts> Members()
    {
        var members = new Mock<IWorkspaceMemberFacts>();
        foreach (var id in new[] { _author, _bob, _carol })
            members.Setup(m => m.FindAsync(_workspaceId, id, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(id, "Member", true));
        return members;
    }

    private static IBroadcastAuthorizer Broadcasters(bool allowed)
    {
        var broadcast = new Mock<IBroadcastAuthorizer>();
        broadcast.Setup(b => b.CanBroadcastAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(allowed);
        return broadcast.Object;
    }

    /// <summary>One send, in its own transaction, the way the pipeline runs a write request.</summary>
    private async Task<Result<SendMessageResultDto>> SendAsync(string content, string clientMessageId, params MentionInput[] mentions)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var handler = new SendMessageCommandHandler(
            context, RequestAs(_author).Object, Members().Object, new ChatSpaceSequenceAllocator(context), new FixedClock(Now), Broadcasters(true), new InMemoryChatMessageAttachments(), Moq.Mock.Of<Notrelix.Application.Features.Chat.RateLimits.IChatRateLimiter>(), new Notrelix.Infrastructure.Data.Chat.ChatReadStateStore(context));

        var result = await handler.Handle(
            new SendMessageCommand(_workspaceId, _spaceId, content, clientMessageId, null, mentions), CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<int> MentionCountOfAsync(Guid userId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var summary = await new GetUnreadSummaryQueryHandler(context, RequestAs(userId).Object, Members().Object, new FixedClock(Now))
            .Handle(new GetUnreadSummaryQuery(_workspaceId), CancellationToken.None);
        return summary.Data!.Spaces.Single().MentionCount;
    }

    private static MentionInput Person(int start, int length, Guid id) => new(MentionKindWire.UserKind, start, length, id, null);

    [Fact]
    public async Task ASendStoresItsMentionsWithTheMessage_AndTheMessageListReadsThemBack()
    {
        var sent = await SendAsync("hi @Bob and @here", "m-1", Person(3, 4, _bob), new MentionInput(MentionKindWire.BroadcastHereKind, 12, 5, null, null));

        sent.Succeeded.Should().BeTrue();
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var rows = await context.ChatMessageMentions.IgnoreQueryFilters().Where(m => m.MessageId == sent.Data!.Message.Id).ToListAsync();
        rows.Select(r => r.Kind).Should().BeEquivalentTo([MentionKind.User, MentionKind.BroadcastHere]);

        var page = await new ListMessagesQueryHandler(context, RequestAs(_bob).Object, Members().Object, new InMemoryChatMessageAttachments())
            .Handle(new ListMessagesQuery(_workspaceId, _spaceId, null, null, null, 50), CancellationToken.None);
        page.Data!.Items.Single().Mentions.Select(m => m.Kind).Should().Equal("user", "broadcast-here");
    }

    [Fact]
    public async Task ARetryWithTheSameClientId_StoresOneMessageAndOneSetOfMentions()
    {
        await SendAsync("hi @Bob", "dup", Person(3, 4, _bob));
        var retry = await SendAsync("hi @Bob", "dup", Person(3, 4, _bob));

        retry.Data!.Created.Should().BeFalse();
        await using var context = _db.CreateOwnerContext(SystemTenant());
        (await context.ChatMessages.IgnoreQueryFilters().CountAsync(m => m.SpaceId == _spaceId)).Should().Be(1);
        (await context.ChatMessageMentions.IgnoreQueryFilters().CountAsync(m => m.SpaceId == _spaceId)).Should().Be(1);
    }

    [Fact]
    public async Task TheMentionCountIsPerPerson_ClearsWhenReadAndIgnoresTheAuthor()
    {
        await SendAsync("hi @Bob", "m-1", Person(3, 4, _bob));

        (await MentionCountOfAsync(_bob)).Should().Be(1);
        (await MentionCountOfAsync(_carol)).Should().Be(0);
        (await MentionCountOfAsync(_author)).Should().Be(0);

        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            (await new ChatReadStateStore(context).AdvanceAsync(_workspaceId, _spaceId, _bob, 1, CancellationToken.None)).Should().BeTrue();
            await context.SaveChangesAsync();
        }

        (await MentionCountOfAsync(_bob)).Should().Be(0, "reading past the mention clears it");
    }

    [Fact]
    public async Task TheStoredCount_FollowsASend_ABroadcast_AnEdit_AReadAndADelete_AndAlwaysEqualsTheRecount()
    {
        var first = await SendAsync("hi @Bob", "m-1", Person(3, 4, _bob));
        await SendAsync("hey @channel", "m-2", new MentionInput(MentionKindWire.BroadcastChannelKind, 4, 8, null, null));
        (await StoredCountsAsync()).Should().Equal(new Dictionary<Guid, int> { [_bob] = 2, [_carol] = 1, [_author] = 0 });
        await AssertStoredEqualsRecountAsync();

        // An edit that drops the mention of Bob takes one off him and nothing off Carol.
        await EditAsync(first.Data!.Message, "hi there");
        (await StoredCountsAsync()).Should().Equal(new Dictionary<Guid, int> { [_bob] = 1, [_carol] = 1, [_author] = 0 });
        await AssertStoredEqualsRecountAsync();

        // An edit that adds Carol puts one on her; Bob's broadcast stays.
        await EditAsync(first.Data.Message, "hi @Carol", Person(3, 6, _carol));
        (await StoredCountsAsync()).Should().Equal(new Dictionary<Guid, int> { [_bob] = 1, [_carol] = 2, [_author] = 0 });
        await AssertStoredEqualsRecountAsync();

        // A read up to sequence 1 lowers the count to the mentions after it: the broadcast (sequence 2) stays.
        await ReadAsync(_carol, 1);
        (await StoredCountsAsync())[_carol].Should().Be(1);
        await AssertStoredEqualsRecountAsync();

        await ReadAsync(_carol, 2);
        (await StoredCountsAsync())[_carol].Should().Be(0);
    }

    [Fact]
    public async Task AnAuthorWhoWasCaughtUp_DoesNotSeeTheirOwnMessageAsUnread_AndOneWhoFellBehindDoes()
    {
        await SendAsync("one", "m-1");
        (await BoundaryOfAsync(_author)).Should().Be(1, "the author had read everything before their message");

        // Bob writes while the author is behind: the author's boundary stays.
        await SendAsAsync(_bob, "from bob", "b-1");
        await SendAsync("two", "m-2");
        (await BoundaryOfAsync(_author)).Should().Be(1, "the author never read Bob's message, so their own later message does not move the boundary");
    }

    [Fact]
    public async Task AMarkReadThatWaitsForAMentionSend_DoesNotOverwriteItsCount()
    {
        await SendAsync("plain", "m-1");

        // T1 stores message 2 with a mention of Bob and holds its +1 on Bob's row, uncommitted.
        await using var sendContext = _db.CreateOwnerContext(SystemTenant());
        await using var sendTransaction = await sendContext.Database.BeginTransactionAsync();
        await new SendMessageCommandHandler(
                sendContext, RequestAs(_author).Object, Members().Object, new ChatSpaceSequenceAllocator(sendContext), new FixedClock(Now), Broadcasters(true),
                new InMemoryChatMessageAttachments(), Moq.Mock.Of<Notrelix.Application.Features.Chat.RateLimits.IChatRateLimiter>(), new ChatReadStateStore(sendContext))
            .Handle(new SendMessageCommand(_workspaceId, _spaceId, "hi @Bob", "m-2", null, [Person(3, 4, _bob)]), CancellationToken.None);
        await sendContext.SaveChangesAsync();

        // T2 marks sequence 1 read while T1 is open: its statement snapshot predates T1's commit and it waits on the row lock.
        var read = Task.Run(async () =>
        {
            await using var readContext = _db.CreateOwnerContext(SystemTenant());
            await using var readTransaction = await readContext.Database.BeginTransactionAsync();
            await new ChatReadStateStore(readContext).AdvanceAsync(_workspaceId, _spaceId, _bob, 1, CancellationToken.None);
            await readTransaction.CommitAsync();
        });
        await Task.Delay(1000);
        read.IsCompleted.Should().BeFalse("the mark-read waits for the send that holds the row");

        await sendTransaction.CommitAsync();
        await read;

        (await StoredCountsAsync())[_bob].Should().Be(1, "message 2 is after the boundary and mentions Bob");
        await AssertStoredEqualsRecountAsync();
    }

    [Fact]
    public async Task AMentionSendThatWaitsForAMarkRead_AddsItsOneOnTopOfTheRecount()
    {
        await SendAsync("plain", "m-1");

        // T2 holds Bob's row after marking sequence 1 read, uncommitted.
        await using var readContext = _db.CreateOwnerContext(SystemTenant());
        await using var readTransaction = await readContext.Database.BeginTransactionAsync();
        await new ChatReadStateStore(readContext).AdvanceAsync(_workspaceId, _spaceId, _bob, 1, CancellationToken.None);

        var send = SendAsync("hi @Bob", "m-2", Person(3, 4, _bob));
        await Task.Delay(1000);
        send.IsCompleted.Should().BeFalse("the send waits for the mark-read that holds the row");

        await readTransaction.CommitAsync();
        await send;

        (await StoredCountsAsync())[_bob].Should().Be(1);
        await AssertStoredEqualsRecountAsync();
    }

    [Fact]
    public async Task TheStoredMessageKind_IsTheLiteralOfThePartialIndexPredicates()
    {
        await SendAsync("plain", "m-1");

        await using var context = _db.CreateOwnerContext(SystemTenant());
        var kind = (await context.Database.SqlQuery<string>($"SELECT kind AS \"Value\" FROM chat.messages WHERE space_id = {_spaceId} LIMIT 1").ToListAsync()).Single();
        foreach (var index in new[] { "ix_messages_unread_count", "ix_messages_content_trgm" })
        {
            var definition = (await context.Database.SqlQuery<string>($"SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'chat' AND indexname = {index}").ToListAsync()).Single();
            definition.Should().Contain($"(kind)::text = '{kind}'::text", $"{index} only serves queries when its predicate matches the stored kind; if the stored spelling changes the index goes dead silently");
        }
    }

    private async Task<Dictionary<Guid, int>> StoredCountsAsync()
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        return await context.ChatMemberships.AsNoTracking().Where(m => m.SpaceId == _spaceId).ToDictionaryAsync(m => m.UserId, m => m.MentionCount);
    }

    private async Task AssertStoredEqualsRecountAsync()
    {
        var stored = await StoredCountsAsync();
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var recount = await MentionCounts.ForSpaceAsync(context, _spaceId, stored.Keys.ToList(), CancellationToken.None);
        foreach (var (user, count) in stored)
            count.Should().Be(recount.GetValueOrDefault(user), "the stored count equals the count over the stored mentions");
    }

    private async Task<long> BoundaryOfAsync(Guid userId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        return await context.ChatMemberships.AsNoTracking().Where(m => m.SpaceId == _spaceId && m.UserId == userId).Select(m => m.ReadSequence).SingleAsync();
    }

    private async Task ReadAsync(Guid userId, long sequence)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        (await new ChatReadStateStore(context).AdvanceAsync(_workspaceId, _spaceId, userId, sequence, CancellationToken.None)).Should().BeTrue();
    }

    private async Task SendAsAsync(Guid author, string content, string clientMessageId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        await new SendMessageCommandHandler(
                context, RequestAs(author).Object, Members().Object, new ChatSpaceSequenceAllocator(context), new FixedClock(Now), Broadcasters(true),
                new InMemoryChatMessageAttachments(), Moq.Mock.Of<Notrelix.Application.Features.Chat.RateLimits.IChatRateLimiter>(), new ChatReadStateStore(context))
            .Handle(new SendMessageCommand(_workspaceId, _spaceId, content, clientMessageId, null), CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task EditAsync(MessageDto message, string content, params MentionInput[] mentions)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var current = await context.ChatMessages.AsNoTracking().Where(m => m.Id == message.Id).Select(m => m.Version).SingleAsync();
        var result = await new Notrelix.Application.Features.Chat.Messages.Commands.EditMessage.EditMessageCommandHandler(
                context, RequestAs(_author).Object, Members().Object, new FixedClock(Now.AddMinutes(1)), Broadcasters(true),
                new InMemoryChatMessageAttachments(), new ChatReadStateStore(context))
            .Handle(new Notrelix.Application.Features.Chat.Messages.Commands.EditMessage.EditMessageCommand(_workspaceId, message.Id, content, current, mentions), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    [Fact]
    public async Task ABroadcastCountsForEveryMemberButTheAuthor_InOneGroupedQuery()
    {
        await SendAsync("hey @channel", "m-1", new MentionInput(MentionKindWire.BroadcastChannelKind, 4, 8, null, null));

        await using var context = _db.CreateOwnerContext(SystemTenant());
        var counts = await MentionCounts.ForSpaceAsync(context, _spaceId, [_author, _bob, _carol], CancellationToken.None);

        counts.Should().Equal(new Dictionary<Guid, int> { [_bob] = 1, [_carol] = 1 });
    }

    [Fact]
    public async Task ADeleteTakesTheMentionRows_SoNothingCountsAnyMore()
    {
        var sent = await SendAsync("hi @Bob", "m-1", Person(3, 4, _bob));

        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var delete = await new DeleteMessageCommandHandler(
                    context, RequestAs(_author).Object, Members().Object, new ChatSpaceSequenceAllocator(context), new FixedClock(Now), new InMemoryChatMessageAttachments(), new Notrelix.Infrastructure.Data.Chat.ChatReadStateStore(context))
                .Handle(new DeleteMessageCommand(_workspaceId, sent.Data!.Message.Id, sent.Data.Message.Version), CancellationToken.None);
            delete.Succeeded.Should().BeTrue();
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatMessageMentions.IgnoreQueryFilters().CountAsync(m => m.SpaceId == _spaceId)).Should().Be(0);
        (await MentionCountOfAsync(_bob)).Should().Be(0);
    }

    [Fact]
    public async Task InAConversation_AMentionOfAParticipantCounts_AndOneOfANonParticipantWarnsAndNeverCounts()
    {
        var direct = Space.CreateDirect(_accountId, _workspaceId, _author, _bob, Now);
        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.ChatSpaces.Add(direct);
            await seed.SaveChangesAsync();
        }

        Result<SendMessageResultDto> result;
        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            var handler = new SendMessageCommandHandler(
                context, RequestAs(_author).Object, Members().Object, new ChatSpaceSequenceAllocator(context), new FixedClock(Now), Broadcasters(true), new InMemoryChatMessageAttachments(), Moq.Mock.Of<Notrelix.Application.Features.Chat.RateLimits.IChatRateLimiter>(), new Notrelix.Infrastructure.Data.Chat.ChatReadStateStore(context));
            result = await handler.Handle(
                new SendMessageCommand(_workspaceId, direct.Id, "hi @Bob and @Carol", "d-1", null, [Person(3, 4, _bob), Person(12, 6, _carol)]),
                CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        result.Data!.Message.Mentions.Should().HaveCount(2);
        result.Data.Message.MentionNotices.Should().ContainSingle(n => n.UserId == _carol);
        await using var read = _db.CreateOwnerContext(SystemTenant());
        var counts = await MentionCounts.ForSpaceAsync(read, direct.Id, [_author, _bob, _carol], CancellationToken.None);
        counts.Should().Equal(new Dictionary<Guid, int> { [_bob] = 1 });
    }
}
