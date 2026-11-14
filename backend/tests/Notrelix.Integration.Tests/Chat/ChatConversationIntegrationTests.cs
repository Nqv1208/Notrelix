using Notrelix.Application.Features.Chat.Conversations.Commands.StartConversation;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Infrastructure.Data.Chat;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-E01 against PostgreSQL: starting the direct conversation of one pair is idempotent and race-safe. Each start runs in its
/// own context and transaction, the way the pipeline runs a write; the pair advisory lock makes the lookup and the insert one step, so
/// racing starts (from either side of the pair) store one conversation. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatConversationIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private const int Racers = 6;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();

    public ChatConversationIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
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

    private async Task<(bool Created, Guid ConversationId)> StartInOwnTransactionAsync(Guid caller, Guid other)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await using var transaction = await context.Database.BeginTransactionAsync();

        var request = new Mock<ICurrentRequestContext>();
        request.Setup(c => c.UserId).Returns(caller);
        request.Setup(c => c.IsAuthenticated).Returns(true);
        request.Setup(c => c.RequireAccountId()).Returns(_accountId);
        var members = new Mock<IWorkspaceMemberFacts>();
        foreach (var id in new[] { _alice, _bob })
            members.Setup(m => m.FindAsync(_workspaceId, id, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(id, "Member", true));

        var handler = new StartConversationCommandHandler(context, request.Object, members.Object, new ChatDirectConversationLock(context), new FixedClock(Now));
        var result = await handler.Handle(new StartConversationCommand(_workspaceId, "direct", other, null, null), CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return (result.Data!.Created, result.Data.Conversation.Id);
    }

    [Fact]
    public async Task RacingStartsOfOnePair_FromBothSides_StoreOneConversation()
    {
        var outcomes = await Task.WhenAll(Enumerable.Range(0, Racers)
            .Select(i => i % 2 == 0 ? StartInOwnTransactionAsync(_alice, _bob) : StartInOwnTransactionAsync(_bob, _alice)));

        outcomes.Count(o => o.Created).Should().Be(1, "exactly one racer stores the conversation under the pair lock");
        outcomes.Select(o => o.ConversationId).Distinct().Should().ContainSingle("every racer gets the same conversation");
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        (await probe.ChatDirectPairs.IgnoreQueryFilters().CountAsync(p => p.WorkspaceId == _workspaceId)).Should().Be(1);
        (await probe.ChatSpaces.IgnoreQueryFilters().CountAsync(s => s.WorkspaceId == _workspaceId)).Should().Be(1);
        (await probe.ChatMemberships.IgnoreQueryFilters().CountAsync(m => m.WorkspaceId == _workspaceId)).Should().Be(2);
    }

    [Fact]
    public async Task StartingAgainLater_ReturnsTheStoredConversation()
    {
        var first = await StartInOwnTransactionAsync(_alice, _bob);
        var again = await StartInOwnTransactionAsync(_bob, _alice);

        first.Created.Should().BeTrue();
        again.Created.Should().BeFalse();
        again.ConversationId.Should().Be(first.ConversationId);
    }
}
