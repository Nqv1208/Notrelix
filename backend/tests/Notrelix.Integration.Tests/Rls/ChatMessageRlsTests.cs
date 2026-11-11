using Moq;
using Npgsql;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Chat messages are workspace-scoped at the database: another Workspace reads none of this one's messages. Inside the
/// Workspace, a private channel the person is not in stays not found through the application path, because channel
/// visibility is decided by the application, not by the row policy (spec CHT-002, UC-CHT-B02).
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatMessageRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _outsider = null!;
    private Guid _spaceId;

    public ChatMessageRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-msg-a@example.com", "MsgA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-msg-b@example.com", "MsgB", "hash");
        _outsider = await AppRoleMembers.AddAsync(_fixture, _a, "Outsider", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPrivate, "secret", null, null, 1, _a.User.Id, Now);
        _spaceId = channel.Id;
        var message = Message.CreateUserMessage(_a.AccountId, _a.WorkspaceId, _spaceId, 1, _a.User.Id, "private text", "client-rls", null, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
        ctx.ChatMessages.Add(message);
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

    [Fact]
    public async Task OwningWorkspaceReadsItsOwnMessage()
    {
        var count = await AsAppAsync(_a, $"SELECT count(*) FROM chat.messages WHERE space_id = '{_spaceId}'");

        count.Should().Be("1", "the positive control: the owning Workspace reads its own message");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoMessageOfThisOne()
    {
        var count = await AsAppAsync(_b, $"SELECT count(*) FROM chat.messages WHERE space_id = '{_spaceId}'");

        count.Should().Be("0");
    }

    [Fact]
    public async Task InsideTheWorkspaceAPrivateChannelTheCallerIsNotInStaysNotFound()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(_outsider.Id);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        context.Setup(c => c.RequireAccountId()).Returns(_a.AccountId);
        context.Setup(c => c.RequireWorkspaceId()).Returns(_a.WorkspaceId);
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_a.WorkspaceId, _outsider.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider.Id, "Member", true));

        await using var db = _fixture.CreateAppContext(tenant);
        var handler = new ListMessagesQueryHandler(db, context.Object, members.Object);

        var act = () => handler.Handle(
            new ListMessagesQuery(_a.WorkspaceId, _spaceId, null, null, null, 50), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("a private channel the caller is not in is not found");
    }

    [Fact]
    public async Task RowPolicyAloneDoesNotHideAPrivateChannelFromAWorkspaceMember()
    {
        // Documents the boundary: the row policy is per Workspace, so the channel check is the application's job.
        var count = await AsAppAsync(_a, $"SELECT count(*) FROM chat.messages WHERE space_id = '{_spaceId}'");

        count.Should().Be("1");
    }
}
