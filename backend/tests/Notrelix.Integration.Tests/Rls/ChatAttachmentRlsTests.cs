using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Storage;
using Notrelix.Domain.SharedKernel;
using Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentContent;
using Notrelix.Application.Features.Chat.Attachments.Queries.ListMessageAttachments;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Files on a message are rows of the shared Collaboration attachment table with target kind chat.message (CHT-012), so they are
/// Workspace scoped by the table's own policy: another Workspace reads none and cannot write one. Inside the Workspace, a private
/// channel's files stay not found for a non-member through the application path, and the file list of a member is one query over the
/// real table (UC-CHT-G08). Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatAttachmentRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _outsider = null!;
    private Guid _spaceId;
    private Guid _messageId;
    private Guid _attachmentId;

    public ChatAttachmentRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-attach-a@example.com", "AttachA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-attach-b@example.com", "AttachB", "hash");
        _outsider = await AppRoleMembers.AddAsync(_fixture, _a, "AttachOutsider", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        var channel = Space.CreateChannel(_a.AccountId, _a.WorkspaceId, SpaceKind.ChannelPrivate, "attach-secret", null, null, 1, _a.User.Id, Now);
        _spaceId = channel.Id;
        var message = Message.CreateUserMessage(_a.AccountId, _a.WorkspaceId, _spaceId, 1, _a.User.Id, "private text", "rls-attachment-message", null, Now);
        _messageId = message.Id;
        var attachment = Attachment.CreateStoredFile(
            _a.AccountId, _a.WorkspaceId, ResourceRef.Create(AttachmentTargets.ChatMessage, _messageId, _a.WorkspaceId), AttachmentType.Document,
            "secret.pdf", 12, "application/pdf", $"{_a.AccountId:N}/{_a.WorkspaceId:N}/attachments/{Guid.NewGuid():N}", 1024 * 1024, 0, 10, _a.User.Id, Now);
        _attachmentId = attachment.Id;

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatSpaces.Add(channel);
        ctx.ChatMessages.Add(message);
        ctx.Attachments.Add(attachment);
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

    private static Mock<ICurrentRequestContext> RequestFor(Guid userId, Guid accountId, Guid workspaceId)
    {
        var context = new Mock<ICurrentRequestContext>();
        context.Setup(c => c.UserId).Returns(userId);
        context.Setup(c => c.IsAuthenticated).Returns(true);
        context.Setup(c => c.RequireAccountId()).Returns(accountId);
        context.Setup(c => c.RequireWorkspaceId()).Returns(workspaceId);
        return context;
    }

    private Mock<IWorkspaceMemberFacts> MembersFor(Guid userId)
    {
        var members = new Mock<IWorkspaceMemberFacts>();
        members.Setup(m => m.FindAsync(_a.WorkspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, "Member", true));
        return members;
    }

    private ChatMessageAttachments RealFlow(Notrelix.Infrastructure.Data.ApplicationDbContext db, Mock<ICurrentRequestContext> context) => new(
        db, context.Object, new FixedClock(Now), new Mock<IStorageService>().Object, new Mock<IRequestRollbackCompensations>().Object,
        new InMemoryAttachmentTargetLock(), Options.Create(new AttachmentUploadOptions()), NullLogger<ChatMessageAttachments>.Instance);

    [Fact]
    public async Task OwningWorkspaceReadsItsOwnFile()
    {
        var count = await AsAppAsync(_a, $"SELECT count(*) FROM collab.attachments WHERE resource_type = 'chat.message' AND resource_id = '{_messageId}'");

        count.Should().Be("1", "the positive control: the owning Workspace reads its own file row");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoFile()
    {
        var count = await AsAppAsync(_b, $"SELECT count(*) FROM collab.attachments WHERE resource_type = 'chat.message' AND resource_id = '{_messageId}'");

        count.Should().Be("0");
    }

    [Fact]
    public async Task AMemberReadsTheFileListOfAPrivateChannelThroughTheRealTable()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _a.User.Id);
        var context = RequestFor(_a.User.Id, _a.AccountId, _a.WorkspaceId);

        // The owner connection skips the row policy (proved by the SQL tests above), so this proves the Chat and Collaboration queries
        // themselves over the real tables, not an empty result from a closed policy.
        await using var db = _fixture.CreateOwnerContext(tenant);
        var handler = new ListMessageAttachmentsQueryHandler(db, context.Object, MembersFor(_a.User.Id).Object, RealFlow(db, context));

        var result = await handler.Handle(new ListMessageAttachmentsQuery(_a.WorkspaceId, _messageId), CancellationToken.None);

        var file = result.Data!.Should().ContainSingle().Subject;
        file.Id.Should().Be(_attachmentId);
        file.FileName.Should().Be("secret.pdf");
    }

    [Fact]
    public async Task InsideTheWorkspaceANonMemberCannotListOrDownloadTheFilesOfAPrivateChannel()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _outsider.Id);
        var context = RequestFor(_outsider.Id, _a.AccountId, _a.WorkspaceId);
        await using var db = _fixture.CreateOwnerContext(tenant);
        var flow = RealFlow(db, context);
        var members = MembersFor(_outsider.Id);

        var list = () => new ListMessageAttachmentsQueryHandler(db, context.Object, members.Object, flow)
            .Handle(new ListMessageAttachmentsQuery(_a.WorkspaceId, _messageId), CancellationToken.None);
        var content = () => new GetMessageAttachmentContentQueryHandler(db, context.Object, members.Object, flow)
            .Handle(new GetMessageAttachmentContentQuery(_a.WorkspaceId, _messageId, _attachmentId), CancellationToken.None);

        await list.Should().ThrowAsync<NotFoundException>("a private channel the caller is not in is not found");
        await content.Should().ThrowAsync<NotFoundException>();
    }

    private sealed class FixedClock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }
}
