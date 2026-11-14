using Notrelix.Application.Features.Chat.RateLimits;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-H08 against PostgreSQL: the slow mode column survives a round trip (expand-only, default 0), and the slow mode check
/// reads the person's last message in the space through the real query. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatSlowModeIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _manager = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private Guid _spaceId;

    public ChatSlowModeIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "slow", null, null, 1, _manager, Now);
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
    public async Task ANewChannelStoresZero_AndASetValueIsReadBack()
    {
        await using (var read = _db.CreateOwnerContext(SystemTenant()))
            (await read.ChatSpaces.SingleAsync(s => s.Id == _spaceId)).SlowModeSeconds.Should().Be(0);

        await using (var write = _db.CreateOwnerContext(SystemTenant()))
        {
            var space = await write.ChatSpaces.SingleAsync(s => s.Id == _spaceId);
            space.SetSlowMode(45, _manager, Now);
            await write.SaveChangesAsync();
        }

        await using var again = _db.CreateOwnerContext(SystemTenant());
        (await again.ChatSpaces.SingleAsync(s => s.Id == _spaceId)).SlowModeSeconds.Should().Be(45);
    }

    [Fact]
    public async Task TheSlowModeCheck_HoldsAPersonByTheirOwnLastMessage_AndNoOneElse()
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var space = await context.ChatSpaces.SingleAsync(s => s.Id == _spaceId);
        space.SetSlowMode(30, _manager, Now);
        context.ChatMessages.Add(Message.CreateUserMessage(_accountId, _workspaceId, _spaceId, 1, _member, "hi", "c-1", null, Now));
        await context.SaveChangesAsync();

        var held = await FluentActions.Awaiting(() => SlowMode.RequireAsync(context, space, _member, Now.AddSeconds(10), CancellationToken.None))
            .Should().ThrowAsync<TooManyRequestsException>();
        held.Which.RuleCode.Should().Be(ChatRuleCodes.SlowMode);
        held.Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(20));

        await SlowMode.RequireAsync(context, space, _member, Now.AddSeconds(30), CancellationToken.None);
        await SlowMode.RequireAsync(context, space, _manager, Now.AddSeconds(1), CancellationToken.None);
    }
}
