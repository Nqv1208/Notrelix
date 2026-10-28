using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Realtime;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;
using Notrelix.Infrastructure.CrossContext.WorkManagement.Realtime;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// WM-032 producer side. A change to anything on a Board is announced to the Board's viewers by exactly one
/// notify-only realtime fact on the Board stream, written to the outbox in the request's own transaction with
/// the tenant fields. The Board is resolved on PostgreSQL for the Board itself, each Board child, and the
/// approval requests, comments and attachments that point at an item; a page comment and a failed result
/// announce nothing.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardRealtimeNotifierIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private readonly Dictionary<string, Guid> _ids = new();
    private Guid _accountId, _workspaceId, _userId, _boardId;

    public BoardRealtimeNotifierIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Realtime WS", $"rt-{Guid.NewGuid():N}", Now);
        var board = Board.Create(_accountId, workspace.Id, _userId, "Realtime Board", null, Now);
        var group = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        var item = BoardItem.CreateRoot(_accountId, workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Create("a0"), _userId, Now);
        var field = BoardField.Create(_accountId, workspace.Id, board.Id, "Notes", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), _userId, Now);
        var label = Label.Create(_accountId, workspace.Id, board.Id, "Hot", LabelColor.Create("#FF0000"), _userId, Now);
        var checklist = Checklist.Create(_accountId, workspace.Id, item.Id, "Steps", FractionalIndex.Create("a0"), _userId, Now);
        var checklistItem = checklist.AddItem("One", FractionalIndex.Create("a0"), _userId, Now);
        var itemRef = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), item.Id, workspace.Id);
        var approval = ApprovalRequest.Create(_accountId, workspace.Id, itemRef, "Review", _userId, Now);
        var comment = Comment.Create(_accountId, workspace.Id, itemRef, "Hello", _userId, Now);
        var pageComment = Comment.Create(_accountId, workspace.Id,
            ResourceRef.Create(ResourceKind.Create("documents.page"), Guid.NewGuid(), workspace.Id), "On a page", _userId, Now);
        var attachment = Attachment.Create(_accountId, workspace.Id, itemRef, AttachmentType.Document,
            FileMetadata.Create("spec.pdf", 10, "application/pdf", url: "https://example.com/spec.pdf"), _userId, Now);

        (_workspaceId, _boardId) = (workspace.Id, board.Id);
        _ids["work-management.board"] = board.Id;
        _ids["work-management.board-item"] = item.Id;
        _ids["work-management.board-group"] = group.Id;
        _ids["work-management.board-field"] = field.Id;
        _ids["work-management.label"] = label.Id;
        _ids["work-management.checklist"] = checklist.Id;
        _ids["work-management.checklist-item"] = checklistItem.Id;
        _ids["work-management.approval-request"] = approval.Id;
        _ids["collaboration.comment"] = comment.Id;
        _ids["collaboration.attachment"] = attachment.Id;
        _ids["page-comment"] = pageComment.Id;

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        seed.BoardFields.Add(field);
        seed.Labels.Add(label);
        seed.Checklists.Add(checklist);
        seed.ApprovalRequests.Add(approval);
        seed.Comments.AddRange(comment, pageComment);
        seed.Attachments.Add(attachment);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("work-management.board")]
    [InlineData("work-management.board-item")]
    [InlineData("work-management.board-group")]
    [InlineData("work-management.board-field")]
    [InlineData("work-management.label")]
    [InlineData("work-management.checklist")]
    [InlineData("work-management.checklist-item")]
    [InlineData("work-management.approval-request")]
    [InlineData("collaboration.comment")]
    [InlineData("collaboration.attachment")]
    public async Task ChangeToAResource_WritesExactlyOneNotifyOnlyFact_OnTheBoardStream(string kind)
    {
        await NotifyAsync(kind, _ids[kind], Result.Success());

        var outbox = await RealtimeOutboxAsync();
        var message = outbox.Should().ContainSingle().Subject;
        message.WorkspaceId.Should().Be(_workspaceId);
        message.AccountId.Should().Be(_accountId);
        message.ActorUserId.Should().Be(_userId);
        message.StreamKey.Should().Be($"board:Board:{_boardId:N}");
        message.ResourceKind.Should().Be("Board");
        message.ResourceId.Should().Be(_boardId);
        message.PayloadJson.RootElement.GetProperty("payload").EnumerateObject().Should().BeEmpty("a fact never carries resource data");
    }

    [Fact]
    public async Task APageComment_AFailedResult_AndAnUnknownResource_WriteNothing()
    {
        await NotifyAsync("collaboration.comment", _ids["page-comment"], Result.Success());
        await NotifyAsync("work-management.board-item", _ids["work-management.board-item"], Result.Failure(new ApplicationError("x", "refused", ApplicationErrorType.BusinessRule)));
        await NotifyAsync("work-management.board-item", Guid.NewGuid(), Result.Success());

        (await RealtimeOutboxAsync()).Should().BeEmpty();
    }

    private async Task NotifyAsync(string kind, Guid id, object response)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant(), CreateOutboxInterceptor(out var collector));
        var execution = new Mock<IExecutionContextReader>();
        execution.Setup(c => c.AccountId).Returns(_accountId);
        execution.Setup(c => c.WorkspaceId).Returns(_workspaceId);
        execution.Setup(c => c.UserId).Returns(_userId);
        execution.Setup(c => c.CorrelationId).Returns(Guid.NewGuid());
        var notifier = new BoardRealtimeNotifier(
            execution.Object, new FixedClock(),
            new BoardRealtimeScopeAdapter(new BoardChildAuthorizationFactsAdapter(context), context, context),
            collector);

        await notifier.NotifyResourceAsync(kind, id, "TestChange", response, CancellationToken.None);
        await context.SaveChangesAsync();
    }

    private async Task<List<MessagingOutboxMessage>> RealtimeOutboxAsync()
    {
        await using var probe = _db.CreateOwnerContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>().IgnoreQueryFilters()
            .Where(m => m.WorkspaceId == _workspaceId && m.MessageName == "realtime.resource-changed")
            .ToListAsync();
    }

    private static DomainEventInterceptor CreateOutboxInterceptor(out IntegrationEventCollector collector)
    {
        collector = new IntegrationEventCollector();
        return new DomainEventInterceptor(
            new FixedClock(),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new CompositeIntegrationEventMapper(new ServiceCollection().BuildServiceProvider()),
            collector);
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
    }
}
