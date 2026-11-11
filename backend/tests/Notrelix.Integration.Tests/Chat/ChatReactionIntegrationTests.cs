using Notrelix.Application.Features.Chat.Reactions.Commands.AddReaction;
using Notrelix.Application.Features.Chat.Reactions.Queries.ListReactors;
using Notrelix.Application.Features.Chat.Reactions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-G06, G07 against PostgreSQL. Each reaction runs in its own context and transaction, the way the pipeline runs a write: the
/// space row lock must make the read and the insert one step, so racing requests for one reaction store one row (the unique index on
/// message, person and kind settles what the lock does not). The summary and the reactor pages are read with the real query
/// translation. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatReactionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private const int Racers = 5;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _second = Guid.NewGuid();
    private Guid _spaceId;

    public ChatReactionIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "reactions", null, null, 1, _member, Now);
        channel.AddMember(_second, MembershipRole.Member, Now);
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

    private Mock<IWorkspaceMemberFacts> ActiveMembers(Guid userId)
    {
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, "Member", true));
        return members;
    }

    /// <summary>Seeds one message from the member (a user message, the next sequence in the space).</summary>
    private async Task<Guid> SeedMessageAsync(string text)
    {
        await using var seed = _db.CreateOwnerContext(SystemTenant());
        var allocator = new ChatSpaceSequenceAllocator(seed);
        var sequence = await allocator.NextAsync(_spaceId, CancellationToken.None);
        var message = Message.CreateUserMessage(_accountId, _workspaceId, _spaceId, sequence, _member, text, $"seed-{Guid.NewGuid():N}", null, Now);
        seed.ChatMessages.Add(message);
        await seed.SaveChangesAsync();
        return message.Id;
    }

    /// <summary>One reaction, in its own transaction. A refused reaction rolls back and reports the rule code.</summary>
    private async Task<(bool Created, string? RuleCode)> ReactInOwnTransactionAsync(Guid messageId, Guid userId, string kind)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var allocator = new ChatSpaceSequenceAllocator(context);
        var handler = new AddReactionCommandHandler(context, RequestAs(userId).Object, ActiveMembers(userId).Object, allocator, new FixedClock(Now));

        try
        {
            var result = await handler.Handle(new AddReactionCommand(_workspaceId, messageId, kind), CancellationToken.None);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (result.Data!.Created, null);
        }
        catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException refused)
        {
            await transaction.RollbackAsync();
            return (false, refused.RuleCode);
        }
    }

    [Fact]
    public async Task RacingRequestsForOneReaction_StoreOneReaction()
    {
        var messageId = await SeedMessageAsync("react to me");

        var outcomes = await Task.WhenAll(Enumerable.Range(0, Racers).Select(_ => ReactInOwnTransactionAsync(messageId, _member, "👍")));

        outcomes.Count(o => o.Created).Should().Be(1, "exactly one racer stores the reaction under the space lock");
        outcomes.Should().OnlyContain(o => o.RuleCode == null);
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatReactions.IgnoreQueryFilters().CountAsync(r => r.MessageId == messageId)).Should().Be(1);
    }

    [Fact]
    public async Task TheSummaryOfAPage_IsReadInOneQuery_WithCountsAndTheViewersOwnMark()
    {
        var first = await SeedMessageAsync("first");
        var second = await SeedMessageAsync("second");
        await ReactInOwnTransactionAsync(first, _member, "🎉");
        await ReactInOwnTransactionAsync(first, _second, "🎉");
        await ReactInOwnTransactionAsync(second, _second, "👀");

        await using var read = _db.CreateOwnerContext(SystemTenant());
        var summaries = await ReactionSummaries.LoadAsync(read, _workspaceId, _member, [first, second], CancellationToken.None);

        summaries[first].Should().ContainSingle().Which.Should().Be(new Notrelix.Application.Features.Chat.Reactions.DTOs.ReactionSummaryDto("🎉", 2, ReactedByMe: true));
        summaries[second].Should().ContainSingle().Which.Should().Be(new Notrelix.Application.Features.Chat.Reactions.DTOs.ReactionSummaryDto("👀", 1, ReactedByMe: false));
    }

    [Fact]
    public async Task TheReactorPages_FollowTheCursorInEarliestFirstOrder()
    {
        var messageId = await SeedMessageAsync("who");
        await ReactInOwnTransactionAsync(messageId, _member, "👍");
        await ReactInOwnTransactionAsync(messageId, _second, "🚀");
        await ReactInOwnTransactionAsync(messageId, _member, "🎉");

        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        await using var read = _db.CreateOwnerContext(SystemTenant());
        var handler = new ListReactorsQueryHandler(read, RequestAs(_member).Object, members.Object);

        var page1 = await handler.Handle(new ListReactorsQuery(_workspaceId, messageId, null, null, 2), CancellationToken.None);
        var page2 = await handler.Handle(new ListReactorsQuery(_workspaceId, messageId, null, page1.Data!.NextCursor, 2), CancellationToken.None);

        page1.Data!.Items.Should().HaveCount(2);
        page2.Data!.Items.Should().ContainSingle();
        page2.Data.NextCursor.Should().BeNull();
        page1.Data.Items.Select(r => r.Emoji).Concat(page2.Data.Items.Select(r => r.Emoji))
            .Should().Equal("👍", "🚀", "🎉");
    }

    [Fact]
    public async Task DeletingAMessage_RemovesItsReactions()
    {
        var messageId = await SeedMessageAsync("delete me");
        await ReactInOwnTransactionAsync(messageId, _member, "❤️");
        await ReactInOwnTransactionAsync(messageId, _second, "👀");

        await using (var delete = _db.CreateOwnerContext(SystemTenant()))
        {
            var rows = await delete.ChatReactions.Where(r => r.MessageId == messageId).ToListAsync();
            delete.ChatReactions.RemoveRange(rows);
            await delete.SaveChangesAsync();
        }

        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatReactions.IgnoreQueryFilters().CountAsync(r => r.MessageId == messageId)).Should().Be(0);
    }

    [Fact]
    public async Task AReactionOfAKindOffThePalette_IsRefusedByTheDomain()
    {
        var messageId = await SeedMessageAsync("palette");

        var outcome = await ReactInOwnTransactionAsync(messageId, _member, "🔥");

        outcome.RuleCode.Should().Be(ChatRuleCodes.ReactionKindInvalid);
    }
}
