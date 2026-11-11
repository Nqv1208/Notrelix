using Npgsql;
using Notrelix.Application.Features.Chat.Threads.Commands.FollowThread;
using Notrelix.Application.Features.Chat.Threads.Commands.ReplyInThread;
using Notrelix.Application.Features.Chat.Threads.Queries.ListFollowedThreads;
using Notrelix.Application.Features.Chat.Threads.Queries.ListThreadReplies;
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
/// Threads, replies and follows are workspace-scoped at the database: another Workspace reads none of them and cannot write
/// into this one. Inside the Workspace, a private channel's thread that the caller is not a member of stays not found through
/// the application path, because channel visibility is decided by the application (spec CHT-002, CHT-004, UC-CHT-F03). Needs
/// Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatThreadRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _outsider = null!;
    private Guid _spaceId;
    private Guid _rootId;

    public ChatThreadRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-thread-a@example.com", "ThreadA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-thread-b@example.com", "ThreadB", "hash");
        _outsider = await AppRoleMembers.AddAsync(_fixture, _a, "Outsider", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        // A private channel whose only member is the owner-tenant user; the outsider is a Workspace member but not in it.
        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPrivate, "secret", null, null, 1, _a.User.Id, Now);
        _spaceId = channel.Id;
        var root = Message.CreateUserMessage(_a.AccountId, _a.WorkspaceId, _spaceId, 1, _a.User.Id, "private root", "rls-root", null, Now);
        _rootId = root.Id;
        var reply = Message.CreateThreadReply(_a.AccountId, _a.WorkspaceId, _spaceId, 2, _rootId, 1, _a.User.Id, "private reply", "rls-reply", false, Now);
        var thread = MessageThread.Open(_a.AccountId, _a.WorkspaceId, _spaceId, _rootId, Now);
        thread.RecordReply(Now);
        var follower = ThreadFollower.Follow(_a.AccountId, _a.WorkspaceId, _rootId, _a.User.Id, muted: false, readReplySequence: 1);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
        ctx.ChatMessages.AddRange(root, reply);
        ctx.ChatThreads.Add(thread);
        ctx.ChatThreadFollowers.Add(follower);
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
    public async Task OwningWorkspaceReadsItsOwnThreadAndItsFollow()
    {
        var threads = await AsAppAsync(_a, $"SELECT count(*) FROM chat.threads WHERE root_message_id = '{_rootId}'");
        var followers = await AsAppAsync(_a, $"SELECT count(*) FROM chat.thread_followers WHERE root_message_id = '{_rootId}'");

        threads.Should().Be("1", "the positive control: the owning Workspace reads its own thread");
        followers.Should().Be("1");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoThreadAndNoFollow()
    {
        var threads = await AsAppAsync(_b, $"SELECT count(*) FROM chat.threads WHERE root_message_id = '{_rootId}'");
        var followers = await AsAppAsync(_b, $"SELECT count(*) FROM chat.thread_followers WHERE root_message_id = '{_rootId}'");

        threads.Should().Be("0");
        followers.Should().Be("0");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotWriteAFollowIntoThisWorkspace()
    {
        var act = () => AsAppExecuteAsync(_b,
            $"INSERT INTO chat.thread_followers (root_message_id, user_id, account_id, workspace_id, muted, read_reply_sequence) " +
            $"VALUES ('{_rootId}', '{Guid.NewGuid()}', '{_a.AccountId}', '{_a.WorkspaceId}', false, 0)");

        await act.Should().ThrowAsync<PostgresException>("the row policy refuses a row for another Workspace");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotChangeAThreadOfThisWorkspace()
    {
        var changed = await AsAppExecuteAsync(_b,
            $"UPDATE chat.threads SET reply_count = 99 WHERE root_message_id = '{_rootId}'");

        changed.Should().Be(0, "the row policy hides the thread, so nothing is updated");
        var count = await AsAppAsync(_a, $"SELECT reply_count FROM chat.threads WHERE root_message_id = '{_rootId}'");
        count.Should().Be("1", "the owning Workspace still sees its own count");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonMemberOfAPrivateChannelCannotReadTheReplies()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = RequestFor(_outsider.Id);
        var members = MembersFor(_outsider.Id);

        await using var db = _fixture.CreateAppContext(tenant);
        var handler = new ListThreadRepliesQueryHandler(db, context.Object, members.Object, new FixedClock(Now), new InMemoryChatMessageAttachments());

        var act = () => handler.Handle(new ListThreadRepliesQuery(_a.WorkspaceId, _rootId, null, 50), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("a private channel the caller is not in is not found");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonMemberCannotReplyFollowOrListTheFollowedThreadsOfTheChannel()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = RequestFor(_outsider.Id);
        var members = MembersFor(_outsider.Id);

        await using var db = _fixture.CreateAppContext(tenant);

        var reply = () => new ReplyInThreadCommandHandler(db, context.Object, members.Object, new FakeSequences(), new FixedClock(Now), new InMemoryChatMessageAttachments())
            .Handle(new ReplyInThreadCommand(_a.WorkspaceId, _rootId, "sneak", "sneak-1", false), CancellationToken.None);
        var follow = () => new FollowThreadCommandHandler(db, context.Object, members.Object)
            .Handle(new FollowThreadCommand(_a.WorkspaceId, _rootId, true, false), CancellationToken.None);
        var listed = await new ListFollowedThreadsQueryHandler(db, context.Object, members.Object, new FixedClock(Now), new InMemoryChatMessageAttachments())
            .Handle(new ListFollowedThreadsQuery(_a.WorkspaceId, null, 50), CancellationToken.None);

        await reply.Should().ThrowAsync<NotFoundException>();
        await follow.Should().ThrowAsync<NotFoundException>();
        listed.Data!.Items.Should().BeEmpty("the outsider follows nothing in this Workspace");
    }

    [Fact]
    public async Task RowPolicyAloneDoesNotHideAPrivateThreadFromAWorkspaceMember()
    {
        // Documents the boundary: the row policy is per Workspace, so the channel check is the application's job.
        var count = await AsAppAsync(_a, $"SELECT count(*) FROM chat.threads WHERE root_message_id = '{_rootId}'");

        count.Should().Be("1");
    }

    private static Mock<ICurrentRequestContext> RequestFor(Guid userId)
    {
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(userId);
        context.Setup(c => c.IsAuthenticated).Returns(true);
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

    private sealed class FakeSequences : Notrelix.Application.Features.Chat.Abstractions.ISpaceSequenceAllocator
    {
        public Task LockAsync(Guid spaceId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<long> NextAsync(Guid spaceId, CancellationToken cancellationToken) => Task.FromResult(99L);
    }
}
