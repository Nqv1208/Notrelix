using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Domain.Chat.Permissions;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-H01 against PostgreSQL: the overrides of a channel are stored as rows keyed by (space, role, action), a set replaces the
/// stored set in place (a flip, a removal and an addition in one save), the overrides load with the channel, and a second writer that
/// read an older version of the channel is refused by the version token, which the pipeline answers as 412. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatPermissionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private Guid _spaceId;

    public ChatPermissionIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "announcements", null, null, 1, _manager, Now);
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

    private static ChannelPermissionEntry Deny(ChannelPermissionSubject subject, ChannelAction action) =>
        new(subject, action, ChannelPermissionEffect.Deny);

    [Fact]
    public async Task ASetIsStoredLoadedWithTheChannelAndReplacedInPlace()
    {
        await using (var write = _db.CreateOwnerContext(SystemTenant()))
        {
            var space = await ChannelAccess.LoadChannelAsync(write, _workspaceId, _spaceId, CancellationToken.None);
            space.SetPermissionOverrides(
                [Deny(ChannelPermissionSubject.Member, ChannelAction.Post), Deny(ChannelPermissionSubject.Guest, ChannelAction.Post)], _manager, Now);
            await write.SaveChangesAsync();
        }

        await using (var read = _db.CreateOwnerContext(SystemTenant()))
        {
            var space = await ChannelAccess.LoadChannelAsync(read, _workspaceId, _spaceId, CancellationToken.None);
            space.PermissionOverrides.Should().HaveCount(2);
            space.Version.Should().Be(2);
        }

        // A flip, a removal and an addition in one save.
        await using (var replace = _db.CreateOwnerContext(SystemTenant()))
        {
            var space = await ChannelAccess.LoadChannelAsync(replace, _workspaceId, _spaceId, CancellationToken.None);
            space.SetPermissionOverrides(
            [
                new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Post, ChannelPermissionEffect.Allow),
                Deny(ChannelPermissionSubject.Member, ChannelAction.React),
            ], _manager, Now);
            await replace.SaveChangesAsync();
        }

        await using var check = _db.CreateOwnerContext(SystemTenant());
        var rows = await check.ChatPermissionOverrides.AsNoTracking().Where(o => o.SpaceId == _spaceId).ToListAsync();
        rows.Select(r => r.ToEntry()).Should().BeEquivalentTo(new[]
        {
            new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Post, ChannelPermissionEffect.Allow),
            Deny(ChannelPermissionSubject.Member, ChannelAction.React),
        });
    }

    [Fact]
    public async Task AClearRemovesEveryRow()
    {
        await using (var write = _db.CreateOwnerContext(SystemTenant()))
        {
            var space = await ChannelAccess.LoadChannelAsync(write, _workspaceId, _spaceId, CancellationToken.None);
            space.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, Now);
            await write.SaveChangesAsync();
        }
        await using (var clear = _db.CreateOwnerContext(SystemTenant()))
        {
            var space = await ChannelAccess.LoadChannelAsync(clear, _workspaceId, _spaceId, CancellationToken.None);
            space.SetPermissionOverrides([], _manager, Now);
            await clear.SaveChangesAsync();
        }

        await using var check = _db.CreateOwnerContext(SystemTenant());
        (await check.ChatPermissionOverrides.CountAsync(o => o.SpaceId == _spaceId)).Should().Be(0);
    }

    [Fact]
    public async Task ASecondWriterThatReadAnOlderVersionIsRefused()
    {
        await using var first = _db.CreateOwnerContext(SystemTenant());
        await using var second = _db.CreateOwnerContext(SystemTenant());
        var a = await ChannelAccess.LoadChannelAsync(first, _workspaceId, _spaceId, CancellationToken.None);
        var b = await ChannelAccess.LoadChannelAsync(second, _workspaceId, _spaceId, CancellationToken.None);

        a.SetPermissionOverrides([Deny(ChannelPermissionSubject.Member, ChannelAction.Post)], _manager, Now);
        await first.SaveChangesAsync();
        b.SetPermissionOverrides([Deny(ChannelPermissionSubject.Guest, ChannelAction.Post)], _manager, Now);

        var act = () => second.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("the version token refuses a write based on a stale read");
    }
}
