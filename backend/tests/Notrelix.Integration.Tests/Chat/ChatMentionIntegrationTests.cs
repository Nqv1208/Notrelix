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
            context, RequestAs(_author).Object, Members().Object, new ChatSpaceSequenceAllocator(context), new FixedClock(Now), Broadcasters(true), new InMemoryChatMessageAttachments(), Moq.Mock.Of<Notrelix.Application.Features.Chat.RateLimits.IChatRateLimiter>());

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
                    context, RequestAs(_author).Object, Members().Object, new ChatSpaceSequenceAllocator(context), new FixedClock(Now), new InMemoryChatMessageAttachments())
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
                context, RequestAs(_author).Object, Members().Object, new ChatSpaceSequenceAllocator(context), new FixedClock(Now), Broadcasters(true), new InMemoryChatMessageAttachments(), Moq.Mock.Of<Notrelix.Application.Features.Chat.RateLimits.IChatRateLimiter>());
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
