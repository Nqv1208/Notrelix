using Npgsql;
using Notrelix.Application.Features.Chat.Reactions.Commands.AddReaction;
using Notrelix.Application.Features.Chat.Reactions.Commands.RemoveReaction;
using Notrelix.Application.Features.Chat.Reactions.Queries.ListReactors;
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
/// Reactions are workspace-scoped at the database: another Workspace reads none of them and cannot write one into this Workspace.
/// Inside the Workspace, a private channel's reactions and reactors stay not found for a non-member through the application path
/// (UC-CHT-G06, G07, CHT-004). Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatReactionRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _outsider = null!;
    private Guid _spaceId;
    private Guid _messageId;

    public ChatReactionRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-reaction-a@example.com", "ReactA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-reaction-b@example.com", "ReactB", "hash");
        _outsider = await AppRoleMembers.AddAsync(_fixture, _a, "ReactOutsider", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        // A private channel whose only member is the owner-tenant user; the outsider is a Workspace member but not in it.
        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPrivate, "reacted-secret", null, null, 1, _a.User.Id, Now);
        _spaceId = channel.Id;
        var message = Message.CreateUserMessage(_a.AccountId, _a.WorkspaceId, _spaceId, 1, _a.User.Id, "private reacted text", "rls-reaction-message", null, Now);
        _messageId = message.Id;
        var reaction = Reaction.Create(_a.AccountId, _a.WorkspaceId, _spaceId, _messageId, _a.User.Id, "👍", Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
        ctx.ChatMessages.Add(message);
        ctx.ChatReactions.Add(reaction);
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
    public async Task OwningWorkspaceReadsItsOwnReaction()
    {
        var reactions = await AsAppAsync(_a, $"SELECT count(*) FROM chat.reactions WHERE message_id = '{_messageId}'");

        reactions.Should().Be("1", "the positive control: the owning Workspace reads its own reaction");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoReaction()
    {
        var reactions = await AsAppAsync(_b, $"SELECT count(*) FROM chat.reactions WHERE message_id = '{_messageId}'");

        reactions.Should().Be("0");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotWriteAReactionIntoThisWorkspace()
    {
        var act = () => AsAppExecuteAsync(_b,
            $"INSERT INTO chat.reactions (id, account_id, workspace_id, space_id, message_id, user_id, kind, created_at) " +
            $"VALUES ('{Guid.NewGuid()}', '{_a.AccountId}', '{_a.WorkspaceId}', '{_spaceId}', '{_messageId}', '{_b.User.Id}', '👍', now())");

        await act.Should().ThrowAsync<Npgsql.PostgresException>("the row policy refuses a reaction row for another Workspace");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotRemoveAReactionOfThisWorkspace()
    {
        var removed = await AsAppExecuteAsync(_b, $"DELETE FROM chat.reactions WHERE message_id = '{_messageId}'");

        removed.Should().Be(0, "the row policy hides the reaction, so nothing is deleted");
        var still = await AsAppAsync(_a, $"SELECT count(*) FROM chat.reactions WHERE message_id = '{_messageId}'");
        still.Should().Be("1");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonMemberCannotListTheReactorsOfAPrivateChannel()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = RequestFor(_outsider.Id);
        var members = MembersFor(_outsider.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var handler = new ListReactorsQueryHandler(db, context.Object, members.Object);

        var act = () => handler.Handle(new ListReactorsQuery(_a.WorkspaceId, _messageId, null, null, 50), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("a private channel the caller is not in is not found");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonMemberCannotReactOrRemoveInAPrivateChannel()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = RequestFor(_outsider.Id);
        var members = MembersFor(_outsider.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var sequences = new SequenceNoop();
        var clock = new FixedClock(Now);

        var add = () => new AddReactionCommandHandler(db, context.Object, members.Object, sequences, clock)
            .Handle(new AddReactionCommand(_a.WorkspaceId, _messageId, "👍"), CancellationToken.None);
        var remove = () => new RemoveReactionCommandHandler(db, context.Object, members.Object, sequences, clock)
            .Handle(new RemoveReactionCommand(_a.WorkspaceId, _messageId, "👍"), CancellationToken.None);

        await add.Should().ThrowAsync<NotFoundException>();
        await remove.Should().ThrowAsync<NotFoundException>();
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
