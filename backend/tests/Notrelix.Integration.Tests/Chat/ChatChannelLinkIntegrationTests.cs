using Notrelix.Domain.Chat.Spaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-H10 against PostgreSQL: the link columns survive a round trip (expand-only, null by default) and the reverse lookup finds the
/// channel by kind and id. RLS is unchanged: the columns sit on chat.spaces, already scoped by Workspace. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatChannelLinkIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private Guid _spaceId;

    public ChatChannelLinkIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "linked", null, null, 1, _manager, Now);
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

    [Fact]
    public async Task ANewChannelHasNoLink_ASetLinkIsReadBack_AndClearingRemovesIt()
    {
        var board = Guid.NewGuid();
        await using (var read = _db.CreateOwnerContext(SystemTenant()))
            (await read.ChatSpaces.SingleAsync(s => s.Id == _spaceId)).HasLink.Should().BeFalse();

        await using (var write = _db.CreateOwnerContext(SystemTenant()))
        {
            var space = await write.ChatSpaces.SingleAsync(s => s.Id == _spaceId);
            space.SetLink(ChannelLinkKinds.Board, board, _manager, Now);
            await write.SaveChangesAsync();
        }

        await using (var again = _db.CreateOwnerContext(SystemTenant()))
        {
            var space = await again.ChatSpaces.SingleAsync(s => s.Id == _spaceId);
            space.LinkKind.Should().Be(ChannelLinkKinds.Board);
            space.LinkId.Should().Be(board);
            (await again.ChatSpaces.CountAsync(s => s.WorkspaceId == _workspaceId && s.LinkKind == ChannelLinkKinds.Board && s.LinkId == board))
                .Should().Be(1);
            space.ClearLink(_manager, Now);
            await again.SaveChangesAsync();
        }

        await using var last = _db.CreateOwnerContext(SystemTenant());
        (await last.ChatSpaces.SingleAsync(s => s.Id == _spaceId)).LinkId.Should().BeNull();
    }
}
