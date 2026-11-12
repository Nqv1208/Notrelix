using Notrelix.Application.Features.Chat.Retention.Services;
using Notrelix.Domain.Chat.Exports;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Retention;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-H05 and H06 against PostgreSQL: the retention rule and the export job survive a round trip, a retention batch tombstones or purges
/// through the real queries (a thread goes as a whole, cascades included), writes its system Audit fact in the same unit of work and
/// announces the space through the outbox, and a repeated batch changes nothing. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatRetentionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _alice = Guid.NewGuid();
    private Guid _spaceId;

    public ChatRetentionIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _owner, Now.AddDays(-300));
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

    private async Task SeedMessagesAsync()
    {
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        var old = Message.CreateUserMessage(_accountId, _workspaceId, _spaceId, 1, _alice, "old words", "c1", null, Now.AddDays(-100));
        var recent = Message.CreateUserMessage(_accountId, _workspaceId, _spaceId, 2, _alice, "recent words", "c2", null, Now.AddDays(-2));
        var root = Message.CreateUserMessage(_accountId, _workspaceId, _spaceId, 3, _alice, "old root", "c3", null, Now.AddDays(-200));
        ctx.ChatMessages.AddRange(old, recent, root);
        await ctx.SaveChangesAsync();

        var thread = MessageThread.Open(_accountId, _workspaceId, _spaceId, root.Id, Now.AddDays(-150));
        var n = thread.RecordReply(Now.AddDays(-150));
        var reply = Message.CreateThreadReply(_accountId, _workspaceId, _spaceId, 4, root.Id, n, _alice, "old reply", "c4", false, Now.AddDays(-150));
        ctx.ChatThreads.Add(thread);
        ctx.ChatThreadFollowers.Add(ThreadFollower.Follow(_accountId, _workspaceId, root.Id, _alice, false));
        ctx.ChatMessages.Add(reply);
        ctx.ChatPins.Add(Pin.Create(_accountId, _workspaceId, _spaceId, old.Id, _owner, Now.AddDays(-99)));
        await ctx.SaveChangesAsync();
    }

    private async Task<RetentionBatchResult> RunBatchAsync(RetentionMode mode)
    {
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        var clock = FakeDateTimeProvider.WithFixedTime(Now);
        var applier = new ChatRetentionApplier(ctx, new InMemoryChatMessageAttachments(), new ChatSystemAudit(ctx, clock), clock);
        var result = await applier.ApplyBatchAsync(new RetentionTarget(_accountId, _workspaceId, 90, mode, _owner), _spaceId, CancellationToken.None);
        await ctx.SaveChangesAsync();
        return result;
    }

    [Fact]
    public async Task ThePolicyAndTheExportJobRoundTrip()
    {
        var policy = RetentionPolicy.CreateDefault(_accountId, _workspaceId, Now);
        policy.Change(90, RetentionMode.Purge, _owner, Now);
        var job = ChatExportJob.Request(_accountId, _workspaceId, _owner, _alice, Now.AddDays(-5), null, Now);
        await using (var write = _db.CreateOwnerContext(SystemTenant()))
        {
            write.ChatRetentionPolicies.Add(policy);
            write.ChatExportJobs.Add(job);
            await write.SaveChangesAsync();
        }

        await using var read = _db.CreateOwnerContext(SystemTenant());
        var storedPolicy = await read.ChatRetentionPolicies.SingleAsync(p => p.WorkspaceId == _workspaceId);
        storedPolicy.Id.Should().Be(_workspaceId);
        storedPolicy.RetentionDays.Should().Be(90);
        storedPolicy.Mode.Should().Be(RetentionMode.Purge);
        var storedJob = await read.ChatExportJobs.SingleAsync(j => j.Id == job.Id);
        storedJob.Status.Should().Be(ChatExportStatus.Queued);
        storedJob.RangeFrom.Should().Be(Now.AddDays(-5));
    }

    [Fact]
    public async Task TombstoneBatch_KeepsTheRowsAsTombstones_WritesOneSystemFact()
    {
        await SeedMessagesAsync();

        var result = await RunBatchAsync(RetentionMode.Tombstone);

        result.Messages.Should().Be(3, "the old message, the old root and the old reply");
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        var messages = await ctx.ChatMessages.ToListAsync();
        messages.Should().HaveCount(4);
        messages.Single(m => m.Content == "recent words").IsDeleted.Should().BeFalse();
        messages.Count(m => m.IsDeleted).Should().Be(3);
        (await ctx.ChatPins.CountAsync()).Should().Be(0);
        var facts = await ctx.EnterpriseAuditLogs.Where(a => a.Action == ChatSystemAudit.RetentionApplied).ToListAsync();
        facts.Should().ContainSingle();
        facts[0].ActorType.Should().Be("System");
        facts[0].MetadataJson.RootElement.GetRawText().Should().NotContain("old words");

        var second = await RunBatchAsync(RetentionMode.Tombstone);
        second.IsEmpty.Should().BeTrue("a repeated run changes nothing");
        await using var again = _db.CreateOwnerContext(SystemTenant());
        (await again.EnterpriseAuditLogs.CountAsync(a => a.Action == ChatSystemAudit.RetentionApplied)).Should().Be(1);
    }

    [Fact]
    public async Task PurgeBatch_DeletesTheRowsAndTheWholeThread_AndKeepsTheRecentMessage()
    {
        await SeedMessagesAsync();

        var result = await RunBatchAsync(RetentionMode.Purge);

        result.Messages.Should().Be(3);
        result.Threads.Should().Be(1);
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        (await ctx.ChatMessages.SingleAsync()).Content.Should().Be("recent words");
        (await ctx.ChatThreads.CountAsync()).Should().Be(0);
        (await ctx.ChatThreadFollowers.CountAsync()).Should().Be(0);
        (await ctx.ChatPins.CountAsync()).Should().Be(0);
        (await ctx.ChatSpaces.SingleAsync()).LastSequence.Should().Be(0, "the space counter is untouched by retention");
    }
}
