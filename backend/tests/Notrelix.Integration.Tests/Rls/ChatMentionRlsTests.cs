using Npgsql;
using Notrelix.Application.Features.Chat.Mentions.Queries.ListMentionables;
using Notrelix.Application.Features.Chat.Mentions.Services;
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
/// Message mentions are workspace-scoped at the database: another Workspace reads none of them and cannot write one into this
/// Workspace. Inside the Workspace, the mention picker of a private channel stays not found for a non-member through the
/// application path (UC-CHT-G01, G02, CHT-004, COL-030). Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatMentionRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _outsider = null!;
    private Guid _spaceId;
    private Guid _messageId;

    public ChatMentionRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-mention-a@example.com", "MentionA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-mention-b@example.com", "MentionB", "hash");
        _outsider = await AppRoleMembers.AddAsync(_fixture, _a, "MentionOutsider", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        // A private channel whose only member is the owner-tenant user; the outsider is a Workspace member but not in it,
        // and is mentioned in it (a warning, not a grant).
        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPrivate, "mentioned-secret", null, null, 1, _a.User.Id, Now);
        _spaceId = channel.Id;
        var message = Message.CreateUserMessage(_a.AccountId, _a.WorkspaceId, _spaceId, 1, _a.User.Id, "hi @Out", "rls-mention-message", null, Now);
        _messageId = message.Id;
        var mention = MessageMention.Create(_a.AccountId, _a.WorkspaceId, _spaceId, _messageId, new MentionSpan(MentionKind.User, 3, 4, _outsider.Id));

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
        ctx.ChatMessages.Add(message);
        ctx.ChatMessageMentions.Add(mention);
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
    public async Task OwningWorkspaceReadsItsOwnMention()
    {
        var mentions = await AsAppAsync(_a, $"SELECT count(*) FROM chat.message_mentions WHERE message_id = '{_messageId}'");

        mentions.Should().Be("1", "the positive control: the owning Workspace reads its own mention");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoMention()
    {
        var mentions = await AsAppAsync(_b, $"SELECT count(*) FROM chat.message_mentions WHERE message_id = '{_messageId}'");

        mentions.Should().Be("0");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotWriteAMentionIntoThisWorkspace()
    {
        var act = () => AsAppExecuteAsync(_b,
            $"INSERT INTO chat.message_mentions (id, account_id, workspace_id, space_id, message_id, kind, target_id, start, length) " +
            $"VALUES ('{Guid.NewGuid()}', '{_a.AccountId}', '{_a.WorkspaceId}', '{_spaceId}', '{_messageId}', 'user', '{_b.User.Id}', 0, 4)");

        await act.Should().ThrowAsync<Npgsql.PostgresException>("the row policy refuses a mention row for another Workspace");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotRemoveAMentionOfThisWorkspace()
    {
        var removed = await AsAppExecuteAsync(_b, $"DELETE FROM chat.message_mentions WHERE message_id = '{_messageId}'");

        removed.Should().Be(0, "the row policy hides the mention, so nothing is deleted");
        var still = await AsAppAsync(_a, $"SELECT count(*) FROM chat.message_mentions WHERE message_id = '{_messageId}'");
        still.Should().Be("1");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonMemberCannotOpenThePickerOfAPrivateChannel()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = RequestFor(_outsider.Id);
        var members = MembersFor(_outsider.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var handler = new ListMentionablesQueryHandler(db, context.Object, members.Object, Mock.Of<IBroadcastAuthorizer>());

        var act = () => handler.Handle(new ListMentionablesQuery(_a.WorkspaceId, _spaceId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("a private channel the caller is not in is not found");
    }

    [Fact]
    public async Task AMentionedNonMemberHasNoUnreadSpaceAndNoMentionCountThroughTheApplicationPath()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var counts = await MentionCounts.ForUserAsync(db, _a.WorkspaceId, _outsider.Id, CancellationToken.None);

        counts.Should().BeEmpty("a mention of a person who cannot see the channel is stored and never counted");
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
}
