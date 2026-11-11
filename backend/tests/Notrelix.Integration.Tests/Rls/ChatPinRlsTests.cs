using Npgsql;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Chat.Pins.Commands.PinMessage;
using Notrelix.Application.Features.Chat.Pins.Commands.UnpinMessage;
using Notrelix.Application.Features.Chat.Pins.Queries.ListPins;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Pins are workspace-scoped at the database: another Workspace reads none of them and cannot write one into this Workspace.
/// Inside the Workspace, a private channel's pins stay not found for a non-member through the application path (UC-CHT-F06,
/// F07, CHT-004). Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatPinRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _outsider = null!;
    private Guid _spaceId;
    private Guid _messageId;

    public ChatPinRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-pin-a@example.com", "PinA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-pin-b@example.com", "PinB", "hash");
        _outsider = await AppRoleMembers.AddAsync(_fixture, _a, "PinOutsider", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        // A private channel whose only member is the owner-tenant user; the outsider is a Workspace member but not in it.
        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPrivate, "pinned-secret", null, null, 1, _a.User.Id, Now);
        _spaceId = channel.Id;
        var message = Message.CreateUserMessage(_a.AccountId, _a.WorkspaceId, _spaceId, 1, _a.User.Id, "private pinned text", "rls-pin-message", null, Now);
        _messageId = message.Id;
        var pin = Pin.Create(_a.AccountId, _a.WorkspaceId, _spaceId, _messageId, _a.User.Id, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
        ctx.ChatMessages.Add(message);
        ctx.ChatPins.Add(pin);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string?> AsAppAsync(AppRoleTenant tenant, string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction))
        {
            scope.Parameters.AddWithValue("u", tenant.User.Id.ToString());
            scope.Parameters.AddWithValue("a", tenant.AccountId.ToString());
            scope.Parameters.AddWithValue("w", tenant.WorkspaceId.ToString());
            await scope.ExecuteNonQueryAsync();
        }

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync();
        return result?.ToString();
    }

    private async Task<int> AsAppExecuteAsync(AppRoleTenant tenant, string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction))
        {
            scope.Parameters.AddWithValue("u", tenant.User.Id.ToString());
            scope.Parameters.AddWithValue("a", tenant.AccountId.ToString());
            scope.Parameters.AddWithValue("w", tenant.WorkspaceId.ToString());
            await scope.ExecuteNonQueryAsync();
        }

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task OwningWorkspaceReadsItsOwnPin()
    {
        var pins = await AsAppAsync(_a, $"SELECT count(*) FROM chat.pins WHERE space_id = '{_spaceId}'");

        pins.Should().Be("1", "the positive control: the owning Workspace reads its own pin");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoPin()
    {
        var pins = await AsAppAsync(_b, $"SELECT count(*) FROM chat.pins WHERE space_id = '{_spaceId}'");

        pins.Should().Be("0");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotWriteAPinIntoThisWorkspace()
    {
        var act = () => AsAppExecuteAsync(_b,
            $"INSERT INTO chat.pins (id, account_id, workspace_id, space_id, message_id, pinned_by, pinned_at) " +
            $"VALUES ('{Guid.NewGuid()}', '{_a.AccountId}', '{_a.WorkspaceId}', '{_spaceId}', '{_messageId}', '{_b.User.Id}', now())");

        await act.Should().ThrowAsync<Npgsql.PostgresException>("the row policy refuses a pin row for another Workspace");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotRemoveAPinOfThisWorkspace()
    {
        var removed = await AsAppExecuteAsync(_b, $"DELETE FROM chat.pins WHERE space_id = '{_spaceId}'");

        removed.Should().Be(0, "the row policy hides the pin, so nothing is deleted");
        var still = await AsAppAsync(_a, $"SELECT count(*) FROM chat.pins WHERE space_id = '{_spaceId}'");
        still.Should().Be("1");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonMemberOfAPrivateChannelCannotListItsPins()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = RequestFor(_outsider.Id);
        var members = MembersFor(_outsider.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var handler = new ListPinsQueryHandler(db, context.Object, members.Object);

        var act = () => handler.Handle(new ListPinsQuery(_a.WorkspaceId, _spaceId, null, 50), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("a private channel the caller is not in is not found");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonMemberCannotPinOrUnpinInAPrivateChannel()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = RequestFor(_outsider.Id);
        var members = MembersFor(_outsider.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var sequences = new SequenceNoop();
        var clock = new FixedClock(Now);

        var pin = () => new PinMessageCommandHandler(db, context.Object, members.Object, sequences,
                new Notrelix.Application.Features.Chat.Messages.Services.ChatSystemMessages(db, sequences, clock), clock)
            .Handle(new PinMessageCommand(_a.WorkspaceId, _messageId), CancellationToken.None);
        var unpin = () => new UnpinMessageCommandHandler(db, context.Object, members.Object, sequences, clock)
            .Handle(new UnpinMessageCommand(_a.WorkspaceId, _messageId), CancellationToken.None);

        await pin.Should().ThrowAsync<NotFoundException>();
        await unpin.Should().ThrowAsync<NotFoundException>();
    }

    private static Mock<ICurrentRequestContext> RequestFor(Guid userId)
    {
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(userId);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        context.Setup(c => c.RequireAccountId()).Returns(Guid.Empty);
        return context;
    }

    private Mock<IWorkspaceMemberFacts> MembersFor(Guid userId)
    {
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_a.WorkspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, "Member", true));
        return members;
    }

    private sealed class FixedClock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class SequenceNoop : Notrelix.Application.Features.Chat.Abstractions.ISpaceSequenceAllocator
    {
        public Task LockAsync(Guid spaceId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<long> NextAsync(Guid spaceId, CancellationToken cancellationToken) => Task.FromResult(99L);
    }
}
