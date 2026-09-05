using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MassTransit;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Events.Accounts;
using Notrelix.Application.Events.Collaboration;
using Notrelix.Application.Events.Documents;
using Notrelix.Application.Events.Identity;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Events.Workspaces;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Infrastructure.Tests.Messaging;

public class TenantContextConsumeFilterScopedEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task WorkspaceMemberAdded_RestoresWorkspaceTenant()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await AssertWorkspaceTenantAsync(new WorkspaceMemberAddedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: accountId,
            WorkspaceId: workspaceId,
            UserId: userId,
            Role: "Member",
            CorrelationId: Guid.CreateVersion7(),
            ActorUserId: userId,
            CausationId: null,
            OccurredAt: Now), accountId, workspaceId);
    }

    [Fact]
    public async Task BoardItemMoved_RestoresWorkspaceTenant()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await AssertWorkspaceTenantAsync(new BoardItemMovedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: accountId,
            ItemId: Guid.CreateVersion7(),
            BoardId: Guid.CreateVersion7(),
            WorkspaceId: workspaceId,
            OldGroupId: Guid.CreateVersion7(),
            NewGroupId: Guid.CreateVersion7(),
            CorrelationId: Guid.CreateVersion7(),
            ActorUserId: userId,
            CausationId: null,
            OccurredAt: Now), accountId, workspaceId);
    }

    [Fact]
    public async Task PageCreated_RestoresWorkspaceTenant()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await AssertWorkspaceTenantAsync(new PageCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: accountId,
            PageId: Guid.CreateVersion7(),
            WorkspaceId: workspaceId,
            Title: "Scoped page",
            ParentId: null,
            CorrelationId: Guid.CreateVersion7(),
            ActorUserId: userId,
            CausationId: null,
            OccurredAt: Now), accountId, workspaceId);
    }

    [Fact]
    public async Task CommentCreated_RestoresWorkspaceTenant()
    {
        var accountId = Guid.CreateVersion7();
        var workspaceId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await AssertWorkspaceTenantAsync(new CommentCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: accountId,
            CommentId: Guid.CreateVersion7(),
            WorkspaceId: workspaceId,
            TargetType: "boardItem",
            TargetId: Guid.CreateVersion7(),
            AuthorId: userId,
            Body: "scoped comment",
            CorrelationId: Guid.CreateVersion7(),
            ActorUserId: userId,
            CausationId: null,
            OccurredAt: Now), accountId, workspaceId);
    }

    [Fact]
    public async Task IdentityRegistrationCompleted_RestoresAccountTenant()
    {
        var accountId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        await AssertAccountTenantAsync(new IdentityRegistrationCompletedIntegrationEventV1(
            EventId: Guid.CreateVersion7(),
            UserId: userId,
            AccountId: accountId,
            Email: "registration@example.com",
            DisplayName: "Registration User",
            AccountName: "Registration User's Account",
            CorrelationId: Guid.CreateVersion7(),
            ActorUserId: userId,
            SourceEventId: null,
            CausationId: null,
            OccurredAt: Now), accountId);
    }

    [Fact]
    public async Task UserRegistered_RestoresSystemTenant()
    {
        await AssertSystemTenantAsync(new UserRegisteredIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            Email: "legacy@example.com",
            DisplayName: "Legacy User",
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: Now));
    }

    [Fact]
    public async Task UserDeactivated_RestoresSystemTenant()
    {
        await AssertSystemTenantAsync(new UserDeactivatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            UserId: Guid.CreateVersion7(),
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: Now));
    }

    [Fact]
    public async Task WorkspaceEvent_WithNullAccountId_IsRejectedBeforeConsumer()
    {
        await AssertRejectedAsync(new PageCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: null,
            PageId: Guid.CreateVersion7(),
            WorkspaceId: Guid.CreateVersion7(),
            Title: "Invalid page",
            ParentId: null,
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: Now));
    }

    [Fact]
    public async Task WorkspaceEvent_WithEmptyAccountId_IsRejectedBeforeConsumer()
    {
        await AssertRejectedAsync(new PageCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: Guid.Empty,
            PageId: Guid.CreateVersion7(),
            WorkspaceId: Guid.CreateVersion7(),
            Title: "Invalid page",
            ParentId: null,
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: Now));
    }

    [Fact]
    public async Task WorkspaceEvent_WithNullWorkspaceId_IsRejectedBeforeConsumer()
    {
        await AssertRejectedAsync(new PageCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: Guid.CreateVersion7(),
            PageId: Guid.CreateVersion7(),
            WorkspaceId: null,
            Title: "Invalid page",
            ParentId: null,
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: Now));
    }

    [Fact]
    public async Task WorkspaceEvent_WithEmptyWorkspaceId_IsRejectedBeforeConsumer()
    {
        await AssertRejectedAsync(new PageCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: Guid.CreateVersion7(),
            PageId: Guid.CreateVersion7(),
            WorkspaceId: Guid.Empty,
            Title: "Invalid page",
            ParentId: null,
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: Now));
    }

    [Fact]
    public async Task AccountEvent_WithNullAccountId_IsRejectedBeforeConsumer()
    {
        await AssertRejectedAsync(new AccountCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: null,
            OwnerUserId: Guid.CreateVersion7(),
            Name: "Invalid account",
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: Now));
    }

    [Fact]
    public async Task AccountEvent_WithEmptyAccountId_IsRejectedBeforeConsumer()
    {
        await AssertRejectedAsync(new AccountCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: Guid.Empty,
            OwnerUserId: Guid.CreateVersion7(),
            Name: "Invalid account",
            CorrelationId: Guid.CreateVersion7(),
            OccurredAt: Now));
    }

    [Fact]
    public async Task NoneEvent_WithTenantValues_StillRunsAsSystem()
    {
        var change = new RealtimeResourceChangedV1(
            eventId: Guid.CreateVersion7(),
            accountId: Guid.CreateVersion7(),
            workspaceId: Guid.CreateVersion7(),
            actorUserId: Guid.CreateVersion7(),
            correlationId: Guid.CreateVersion7(),
            causationId: null,
            occurredAt: Now,
            topicNamespace: "work-management",
            resourceKind: "board-item",
            resourceId: Guid.CreateVersion7(),
            streamKey: "board-item:test",
            streamVersion: 1,
            changeKind: "updated",
            payloadContract: "test",
            payload: JsonDocument.Parse("{}").RootElement);

        await AssertSystemTenantAsync(change);
    }

    [Fact]
    public async Task UnclassifiedIntegrationEvent_IsRejectedBeforeConsumer()
    {
        await AssertRejectedAsync(new UnclassifiedIntegrationEvent(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7()));
    }

    private static async Task AssertWorkspaceTenantAsync<T>(T integrationEvent, Guid accountId, Guid workspaceId)
        where T : class
    {
        var observed = await RunFilterAsync(integrationEvent);

        observed.Account.Should().Be(accountId);
        observed.Workspace.Should().Be(workspaceId);
        observed.IsSystemContext.Should().BeFalse();
    }

    private static async Task AssertAccountTenantAsync<T>(T integrationEvent, Guid accountId)
        where T : class
    {
        var observed = await RunFilterAsync(integrationEvent);

        observed.Account.Should().Be(accountId);
        observed.Workspace.Should().BeNull();
        observed.IsSystemContext.Should().BeFalse();
    }

    private static async Task AssertSystemTenantAsync<T>(T integrationEvent)
        where T : class
    {
        var observed = await RunFilterAsync(integrationEvent);

        observed.Account.Should().BeNull();
        observed.Workspace.Should().BeNull();
        observed.IsSystemContext.Should().BeTrue();
    }

    private static async Task AssertRejectedAsync<T>(T integrationEvent)
        where T : class
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var filter = new TenantContextConsumeFilter<T>(tenant, NullLogger<TenantContextConsumeFilter<T>>.Instance);

        var context = new Mock<ConsumeContext<T>>();
        context.SetupGet(c => c.Message).Returns(integrationEvent);

        var pipe = new Mock<IPipe<ConsumeContext<T>>>();

        var act = () => filter.Send(context.Object, pipe.Object);

        await act.Should().ThrowAsync<IntegrationEventTenantEnvelopeException>();
        pipe.Verify(p => p.Send(It.IsAny<ConsumeContext<T>>()), Times.Never);
        tenant.AccountId.Should().BeNull();
        tenant.WorkspaceId.Should().BeNull();
        tenant.IsSystemContext.Should().BeFalse();
    }

    private static async Task<(Guid? Account, Guid? Workspace, bool IsSystemContext)> RunFilterAsync<T>(T integrationEvent)
        where T : class
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        var filter = new TenantContextConsumeFilter<T>(tenant, NullLogger<TenantContextConsumeFilter<T>>.Instance);

        var context = new Mock<ConsumeContext<T>>();
        context.SetupGet(c => c.Message).Returns(integrationEvent);

        var pipe = new Mock<IPipe<ConsumeContext<T>>>();
        Guid? observedAccount = null;
        Guid? observedWorkspace = null;
        bool observedSystemContext = false;

        pipe.Setup(p => p.Send(It.IsAny<ConsumeContext<T>>()))
            .Callback<ConsumeContext<T>>(_ =>
            {
                observedAccount = tenant.AccountId;
                observedWorkspace = tenant.WorkspaceId;
                observedSystemContext = tenant.IsSystemContext;
            })
            .Returns(Task.CompletedTask);

        await filter.Send(context.Object, pipe.Object);

        return (observedAccount, observedWorkspace, observedSystemContext);
    }

    public sealed record UnclassifiedIntegrationEvent(
        Guid EventId,
        Guid? AccountId,
        Guid? WorkspaceId,
        Guid CorrelationId)
        : IntegrationEvent(
            eventId: EventId,
            messageName: "test.unclassified",
            schemaVersion: 1,
            correlationId: CorrelationId,
            accountId: AccountId,
            workspaceId: WorkspaceId);
}
