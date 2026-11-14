using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Notrelix.Application.Features.Chat.Conversations.Commands.SetConversationVisibility;
using Notrelix.Application.Features.Chat.Conversations.Queries.ListConversations;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Conversations are workspace-scoped at the database: another Workspace reads none of the conversation rows (spaces, pairs, blocks)
/// and cannot write one into this Workspace. Inside the Workspace, a person who is not a participant never sees the conversation
/// (not in the list, not found on a write), and the Workspace owner has no way in either (UC-CHT-E01 to E07, CHT-004). Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatConversationRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _bob = null!;
    private User _outsider = null!;
    private Guid _spaceId;
    private Guid _otherPairSpaceId;

    public ChatConversationRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-dm-a@example.com", "DmA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-dm-b@example.com", "DmB", "hash");
        _bob = await AppRoleMembers.AddAsync(_fixture, _a, "DmBob", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _outsider = await AppRoleMembers.AddAsync(_fixture, _a, "DmOutsider", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        // The direct conversation of the owner-tenant user and Bob; the outsider is in the Workspace but not in it.
        var conversation = Space.CreateDirect(_a.AccountId, _a.WorkspaceId, _a.User.Id, _bob.Id, Now);
        _spaceId = conversation.Id;
        var pair = DirectPair.Create(_a.AccountId, _a.WorkspaceId, _a.User.Id, _bob.Id, _spaceId);
        var others = Space.CreateDirect(_a.AccountId, _a.WorkspaceId, _bob.Id, _outsider.Id, Now);
        _otherPairSpaceId = others.Id;
        var othersPair = DirectPair.Create(_a.AccountId, _a.WorkspaceId, _bob.Id, _outsider.Id, others.Id);
        var block = PersonBlock.Create(_a.AccountId, _a.WorkspaceId, _a.User.Id, _bob.Id, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(conversation);
        ctx.ChatDirectPairs.Add(pair);
        ctx.ChatSpaces.Add(others);
        ctx.ChatDirectPairs.Add(othersPair);
        ctx.ChatBlocks.Add(block);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string?> AsAppAsync(AppRoleTenant tenant, string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ScopeAsync(connection, transaction, tenant);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (await command.ExecuteScalarAsync())?.ToString();
    }

    private async Task<int> AsAppExecuteAsync(AppRoleTenant tenant, string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await ScopeAsync(connection, transaction, tenant);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task ScopeAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, AppRoleTenant tenant)
    {
        await using var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction);
        scope.Parameters.AddWithValue("u", tenant.User.Id.ToString());
        scope.Parameters.AddWithValue("a", tenant.AccountId.ToString());
        scope.Parameters.AddWithValue("w", tenant.WorkspaceId.ToString());
        await scope.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task OwningWorkspaceReadsItsConversationPairAndBlock()
    {
        (await AsAppAsync(_a, $"SELECT count(*) FROM chat.spaces WHERE id = '{_spaceId}'")).Should().Be("1");
        (await AsAppAsync(_a, $"SELECT count(*) FROM chat.direct_pairs WHERE space_id = '{_spaceId}'")).Should().Be("1");
        (await AsAppAsync(_a, "SELECT count(*) FROM chat.blocks")).Should().Be("1");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoConversationPairOrBlock()
    {
        (await AsAppAsync(_b, $"SELECT count(*) FROM chat.spaces WHERE id = '{_spaceId}'")).Should().Be("0");
        (await AsAppAsync(_b, $"SELECT count(*) FROM chat.direct_pairs WHERE space_id = '{_spaceId}'")).Should().Be("0");
        (await AsAppAsync(_b, "SELECT count(*) FROM chat.blocks")).Should().Be("0");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotWriteAPairOrABlockIntoThisWorkspace()
    {
        var pair = () => AsAppExecuteAsync(_b,
            $"INSERT INTO chat.direct_pairs (account_id, workspace_id, user_low, user_high, space_id) " +
            $"VALUES ('{_a.AccountId}', '{_a.WorkspaceId}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', '{_spaceId}')");
        var block = () => AsAppExecuteAsync(_b,
            $"INSERT INTO chat.blocks (account_id, workspace_id, blocker_id, blocked_id, created_at) " +
            $"VALUES ('{_a.AccountId}', '{_a.WorkspaceId}', '{_b.User.Id}', '{Guid.NewGuid()}', now())");

        await pair.Should().ThrowAsync<PostgresException>("the row policy refuses a pair row for another Workspace");
        await block.Should().ThrowAsync<PostgresException>("the row policy refuses a block row for another Workspace");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotDeleteTheConversationOrTheBlock()
    {
        (await AsAppExecuteAsync(_b, $"DELETE FROM chat.direct_pairs WHERE space_id = '{_spaceId}'")).Should().Be(0);
        (await AsAppExecuteAsync(_b, "DELETE FROM chat.blocks")).Should().Be(0);
        (await AsAppAsync(_a, $"SELECT count(*) FROM chat.direct_pairs WHERE space_id = '{_spaceId}'")).Should().Be("1");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonParticipantListsOnlyTheirOwnConversation()
    {
        var (db, context, members) = await AsPersonAsync(_outsider.Id, "Member");
        await using var _ = db;
        var handler = new ListConversationsQueryHandler(db, context.Object, members.Object, new FixedClock(Now));

        var result = await handler.Handle(new ListConversationsQuery(_a.WorkspaceId), CancellationToken.None);

        result.Data!.Select(c => c.Id).Should().BeEquivalentTo([_otherPairSpaceId], "a person lists only the conversations they are in, not the pair of the owner and Bob");
    }

    [Fact]
    public async Task TheWorkspaceOwnerListsOnlyTheirOwnConversations_NotThoseOfOtherPeople()
    {
        var (db, context, members) = await AsPersonAsync(_a.User.Id, "Owner");
        await using var _ = db;

        var result = await new ListConversationsQueryHandler(db, context.Object, members.Object, new FixedClock(Now))
            .Handle(new ListConversationsQuery(_a.WorkspaceId), CancellationToken.None);

        result.Data!.Select(c => c.Id).Should().BeEquivalentTo([_spaceId], "the positive control: a participant lists their conversation, and not Bob's with the outsider");
    }

    [Fact]
    public async Task AParticipantHidesTheirConversation_AsThePositiveControl()
    {
        var (db, context, members) = await AsPersonAsync(_a.User.Id, "Owner");
        await using var _ = db;

        var result = await new SetConversationVisibilityCommandHandler(db, context.Object, members.Object, new FixedClock(Now))
            .Handle(new SetConversationVisibilityCommand(_a.WorkspaceId, _spaceId, true), CancellationToken.None);

        result.Data!.Hidden.Should().BeTrue();
    }

    [Fact]
    public async Task InsideTheWorkspaceANonParticipantCannotHideTheConversation()
    {
        var (db, context, members) = await AsPersonAsync(_outsider.Id, "Member");
        await using var _ = db;
        var clock = new FixedClock(Now);

        var hide = () => new SetConversationVisibilityCommandHandler(db, context.Object, members.Object, clock)
            .Handle(new SetConversationVisibilityCommand(_a.WorkspaceId, _spaceId, true), CancellationToken.None);

        await hide.Should().ThrowAsync<NotFoundException>("a conversation the caller is not in is not found");
    }

    /// <summary>
    /// A context of the application role for one person, with the row-level-security session settings applied inside a transaction the
    /// way the pipeline does: without them the role reads nothing, so a "sees nothing" check would pass for any reason.
    /// </summary>
    private async Task<(Notrelix.Infrastructure.Data.ApplicationDbContext Db, Mock<ICurrentRequestContext> Context, Mock<IWorkspaceMemberFacts> Members)> AsPersonAsync(Guid userId, string role)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, userId);
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(userId);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        context.Setup(c => c.RequireAccountId()).Returns(_a.AccountId);
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_a.WorkspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, role, true));

        var db = _fixture.CreateAppContext(tenant);
        await db.Database.BeginTransactionAsync();
        var rls = new RlsSessionContext(db, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant, NullLogger<RlsSessionContext>.Instance);
        await rls.ApplyAsync(CancellationToken.None);
        return (db, context, members);
    }

    private sealed class FixedClock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }
}
