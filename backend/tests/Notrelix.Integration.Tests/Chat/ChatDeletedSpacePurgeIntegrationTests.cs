using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-H05 on PostgreSQL: the purge of deleted channels runs as the worker login with the RLS policy applied (not the owner). A channel
/// deleted more than 30 days ago goes with its messages, thread, pin and members and leaves one audit fact; one inside the window, one
/// that is live and the messages of both stay; a second pass changes nothing. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatDeletedSpacePurgeIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Dictionary<Guid, string> _fileKeys = [];

    public ChatDeletedSpacePurgeIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private async Task<Guid> SeedChannelAsync(string name, DateTimeOffset? deletedAt)
    {
        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, name, null, null, 1, _owner, Now.AddDays(-100));
        channel.AddMember(_alice, MembershipRole.Member, Now.AddDays(-100));
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        ctx.ChatSpaces.Add(channel);
        await ctx.SaveChangesAsync();

        var root = Message.CreateUserMessage(_accountId, _workspaceId, channel.Id, 1, _alice, "root", $"{name}-1", null, Now.AddDays(-90));
        ctx.ChatMessages.Add(root);
        await ctx.SaveChangesAsync();
        var thread = MessageThread.Open(_accountId, _workspaceId, channel.Id, root.Id, Now.AddDays(-89));
        var n = thread.RecordReply(Now.AddDays(-89));
        ctx.ChatThreads.Add(thread);
        ctx.ChatMessages.Add(Message.CreateThreadReply(_accountId, _workspaceId, channel.Id, 2, root.Id, n, _alice, "reply", $"{name}-2", false, Now.AddDays(-89)));
        ctx.ChatPins.Add(Pin.Create(_accountId, _workspaceId, channel.Id, root.Id, _owner, Now.AddDays(-88)));
        var key = AttachmentStorageKey.Create(_accountId, _workspaceId);
        _fileKeys[channel.Id] = key;
        ctx.Attachments.Add(Attachment.CreateStoredFile(
            _accountId, _workspaceId, ResourceRef.Create(AttachmentTargets.ChatMessage, root.Id, _workspaceId), AttachmentType.Document,
            "plan.pdf", 12, "application/pdf", key, 1024 * 1024, 0, 10, _alice, Now.AddDays(-90)));
        await ctx.SaveChangesAsync();

        if (deletedAt is { } at)
        {
            channel.Delete(_owner, at);
            ctx.ChatSpaces.Update(channel);
            await ctx.SaveChangesAsync();
        }

        return channel.Id;
    }

    private async Task<(int Spaces, int Members, int Messages, int Threads, int Pins)> RowsAsync(Guid space)
    {
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        return (
            await ctx.ChatSpaces.IgnoreQueryFilters().CountAsync(s => s.Id == space),
            await ctx.ChatMemberships.IgnoreQueryFilters().CountAsync(m => m.SpaceId == space),
            await ctx.ChatMessages.IgnoreQueryFilters().CountAsync(m => m.SpaceId == space),
            await ctx.ChatThreads.IgnoreQueryFilters().CountAsync(t => t.SpaceId == space),
            await ctx.ChatPins.IgnoreQueryFilters().CountAsync(p => p.SpaceId == space));
    }

    private async Task<int> FilesAsync(Guid space)
    {
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        return await ctx.Attachments.IgnoreQueryFilters().CountAsync(a => a.Metadata.StorageKey == _fileKeys[space]);
    }

    private readonly Mock<IStorageService> _storage = new();

    private async Task<int> PurgeAsync()
    {
        var tenant = new CurrentTenantContext();
        await using var context = PostgresTestContainer.CreateContextFor(_db.WorkerConnectionString, tenant);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        var purger = new DeletedChatSpacePurger(
            context, tenant, rls, clock.Object, _storage.Object,
            Options.Create(new DeletedChatSpacePurgeOptions { Enabled = true, BatchSize = 10 }),
            NullLogger<DeletedChatSpacePurger>.Instance);
        return await purger.RunOnceAsync(default);
    }

    [Fact]
    public async Task AChannelDeletedOver30DaysAgoGoesWithItsContent_OneInsideTheWindowAndALiveOneStay()
    {
        var expired = await SeedChannelAsync("old-notes", Now.AddDays(-31));
        var recent = await SeedChannelAsync("recent", Now.AddDays(-10));
        var live = await SeedChannelAsync("live", null);

        (await PurgeAsync()).Should().Be(1);

        (await RowsAsync(expired)).Should().Be((0, 0, 0, 0, 0));
        (await FilesAsync(expired)).Should().Be(0, "the files of a purged channel go with it");
        (await FilesAsync(recent)).Should().Be(1);
        (await FilesAsync(live)).Should().Be(1);
        _storage.Verify(s => s.DeleteAsync(_fileKeys[expired], It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once, "only the purged channel's object is deleted");
        (await RowsAsync(recent)).Should().Be((1, 2, 2, 1, 1));
        (await RowsAsync(live)).Should().Be((1, 2, 2, 1, 1));
        await using var ctx = _db.CreateOwnerContext(SystemTenant());
        (await ctx.EnterpriseAuditLogs.CountAsync(a => a.Action == "PurgeChatSpace" && a.ResourceId == expired && a.WorkspaceId == _workspaceId))
            .Should().Be(1);

        (await PurgeAsync()).Should().Be(0, "a purged channel is gone, so a second pass finds nothing");
    }

    [Fact]
    public async Task AFailedObjectDeleteIsLoggedAndTheChannelStaysPurged()
    {
        var expired = await SeedChannelAsync("old-files", Now.AddDays(-31));
        _storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("storage down"));

        (await PurgeAsync()).Should().Be(1);

        (await RowsAsync(expired)).Spaces.Should().Be(0);
        (await FilesAsync(expired)).Should().Be(0);
    }

    [Fact]
    public async Task ARestoreWindowThatIsExactly30DaysIsStillRestorable()
    {
        var edge = await SeedChannelAsync("edge", Now.AddDays(-30));

        (await PurgeAsync()).Should().Be(0);
        (await RowsAsync(edge)).Spaces.Should().Be(1);
    }
}
