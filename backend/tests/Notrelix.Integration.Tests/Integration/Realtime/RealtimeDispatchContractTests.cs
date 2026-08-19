using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.PostCommit;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Governance.Services;
using Notrelix.Infrastructure.Realtime;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Integration.Realtime;

/// <summary>
/// FZ-INF-05 — realtime dispatch contract certification against the real
/// provider stack (PostgreSQL-backed data session, production pipeline order).
///
/// The certified contract is the post-commit dispatch seam:
///   IRealtimeRequest -> PostCommitEnqueueBehavior -> IPostCommitActionQueue
///   -> (after commit) -> IRealtimePublisher.PublishAsync(RealtimeTopic, payload)
///
/// No WebSocket/SignalR transport exists yet (roadmap item); the seam is what
/// production code can depend on today. Certifying it guarantees:
/// - publish happens only after the transaction commits (never before);
/// - handler failure / rollback / authorization denial never publishes;
/// - a failing publisher cannot corrupt the request result (RULE.md §35);
/// - topics are tenant-qualified so clients cannot observe another tenant;
/// - membership (and revoked membership) gates the publish.
/// </summary>
[Collection("Database")]
public sealed class RealtimeDispatchContractTests : IAsyncLifetime
{
    private static readonly Guid AccountA = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private static readonly Guid AccountB = Guid.Parse("B0000000-0000-0000-0000-000000000002");
    private static readonly Guid WorkspaceA1 = Guid.Parse("A0000000-0000-0000-0000-00000000AA01");
    private static readonly Guid WorkspaceA2 = Guid.Parse("A0000000-0000-0000-0000-00000000AA02");
    private static readonly Guid WorkspaceB1 = Guid.Parse("B0000000-0000-0000-0000-00000000BB01");
    private static readonly Guid UserA = Guid.Parse("00000000-0000-0000-0000-0000000000AA");
    private static readonly Guid UserB = Guid.Parse("00000000-0000-0000-0000-0000000000BB");
    private static readonly Guid OtherUser = Guid.Parse("00000000-0000-0000-0000-0000000000CC");
    private static readonly DateTimeOffset FixedTime = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RealtimeDispatchContractTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        return _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ApplicationDbContext CreateContext(ICurrentTenantContext? tenant = null)
        => _db.CreateContext(tenant);

    private static PermissionService CreatePermissionService(ApplicationDbContext context)
    {
        var snapshots = new ResourceAuthorizationSnapshotStore(
            [new BoardAuthorizationSnapshotResolver(context)]);

        return new PermissionService(
            context,
            context,
            snapshots,
            FakeDateTimeProvider.WithFixedTime(FixedTime));
    }

    /// <summary>
    /// The fake realtime request: a transactional command that publishes to a
    /// workspace-qualified topic after commit.
    /// </summary>
    private sealed record FakeRealtimeCommand : IRequest<string>, ITransactionalRequest, IGlobalRequest, IRealtimeRequest
    {
        public RealtimeTopic Topic { get; init; } = new("workspace", "Workspace", WorkspaceA1);
    }

    /// <summary>
    /// The workspace-scoped, permission-requiring variant used to certify the
    /// membership authorization gate in front of the publish.
    /// </summary>
    private sealed record AuthedRealtimeCommand
        : IRequest<string>, ITransactionalRequest, IRealtimeRequest, IWorkspaceRequest, IRequirePermission
    {
        public Guid WorkspaceId { get; init; } = WorkspaceA1;
        public RealtimeTopic Topic { get; init; } = new("workspace", "Workspace", WorkspaceA1);
        public PermissionAction Action => PermissionAction.ViewWorkspace;
        public ResourceRef Resource =>
            ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
    }

    private sealed record PlainTransactionalCommand : IRequest<string>, ITransactionalRequest, IGlobalRequest;

    private sealed class CapturingRealtimePublisher : IRealtimePublisher
    {
        public List<(RealtimeTopic Topic, object? Payload)> Calls { get; } = new();

        public Exception? ThrowOnPublish { get; set; }

        public Task PublishAsync(RealtimeTopic topic, object? payload, CancellationToken cancellationToken)
        {
            if (ThrowOnPublish is not null)
                throw ThrowOnPublish;

            Calls.Add((topic, payload));
            return Task.CompletedTask;
        }
    }

    private static RlsSessionContext CreateRls(ApplicationDbContext context, FakeCurrentTenantContext tenant)
        => new(context, Options.Create(new RlsOptions { SetSessionContext = true }), tenant);

    private EfRequestDataSession CreateDataSession(ApplicationDbContext context, FakeCurrentTenantContext tenant)
        => new(context, CreateRls(context, tenant), NullLogger<EfRequestDataSession>.Instance);

    /// <summary>
    /// Composes the production pipeline slice relevant to realtime dispatch:
    /// PostCommitScope -> DbRequestScope -> [Authorization] -> PostCommitEnqueue -> handler.
    /// </summary>
    private static async Task<string> RunAsync<TRequest>(
        TRequest request,
        IPostCommitActionQueue queue,
        IRequestDataSession dataSession,
        IRealtimePublisher publisher,
        RequestHandlerDelegate<string> handler,
        IAuthorizationDecisionStore? authorizationStore = null,
        ICurrentUser? currentUser = null,
        ICurrentTenantContext? tenant = null)
        where TRequest : notnull, IRequest<string>
    {
        IPipelineBehavior<TRequest, string> pipeline =
            new PostCommitScopeBehavior<TRequest, string>(
                queue, NullLogger<PostCommitScopeBehavior<TRequest, string>>.Instance);

        var dbScope = new DbRequestScopeBehavior<TRequest, string>(
            dataSession, NullLogger<DbRequestScopeBehavior<TRequest, string>>.Instance);

        var enqueue = new PostCommitEnqueueBehavior<TRequest, string>(
            queue, publisher, new Notrelix.Application.Common.Context.ExecutionContext(),
            NullLogger<PostCommitEnqueueBehavior<TRequest, string>>.Instance);

        if (authorizationStore is not null)
        {
            var authorization = new AuthorizationBehavior<TRequest, string>(
                currentUser!, tenant!, authorizationStore,
                NullLogger<AuthorizationBehavior<TRequest, string>>.Instance);

            return await pipeline.Handle(request,
                _ => dbScope.Handle(request,
                    _ => authorization.Handle(request,
                        _ => enqueue.Handle(request, handler, CancellationToken.None),
                        CancellationToken.None),
                    CancellationToken.None),
                CancellationToken.None);
        }

        return await pipeline.Handle(request,
            _ => dbScope.Handle(request,
                _ => enqueue.Handle(request, handler, CancellationToken.None),
                CancellationToken.None),
            CancellationToken.None);
    }

    /// <summary>
    /// Runs the same pipeline slice for a command that is NOT marked
    /// IRealtimeRequest, proving no publish can leak from other request kinds.
    /// </summary>
    private static async Task<string> RunPlainAsync(
        IPostCommitActionQueue queue,
        IRequestDataSession dataSession,
        IRealtimePublisher publisher,
        RequestHandlerDelegate<string> handler)
    {
        IPipelineBehavior<PlainTransactionalCommand, string> pipeline =
            new PostCommitScopeBehavior<PlainTransactionalCommand, string>(
                queue, NullLogger<PostCommitScopeBehavior<PlainTransactionalCommand, string>>.Instance);

        var dbScope = new DbRequestScopeBehavior<PlainTransactionalCommand, string>(
            dataSession, NullLogger<DbRequestScopeBehavior<PlainTransactionalCommand, string>>.Instance);

        var enqueue = new PostCommitEnqueueBehavior<PlainTransactionalCommand, string>(
            queue, publisher, new Notrelix.Application.Common.Context.ExecutionContext(),
            NullLogger<PostCommitEnqueueBehavior<PlainTransactionalCommand, string>>.Instance);

        return await pipeline.Handle(new PlainTransactionalCommand(),
            _ => dbScope.Handle(new PlainTransactionalCommand(),
                _ => enqueue.Handle(new PlainTransactionalCommand(), handler, CancellationToken.None),
                CancellationToken.None),
            CancellationToken.None);
    }

    private static async Task SeedWorkspaceAsync(
        ApplicationDbContext context, Guid accountId, Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        context.Workspaces.Add(Notrelix.Domain.Workspaces.Workspaces.Workspace.Create(
            accountId, userId, "Certification Workspace", $"ws-{workspaceId:N}", FixedTime, null, isPersonal: false));
        context.WorkspaceMembers.Add(WorkspaceMember.Create(
            accountId, workspaceId, userId, role, userId, FixedTime));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task RealtimeCommand_PublishesOnceAfterCommit_WithWorkspaceQualifiedTopic()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seedContext = CreateContext(tenant))
        {
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA1, UserA, WorkspaceRole.Owner);
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserA);
        await using var context = CreateContext(tenant);
        var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
        var publisher = new CapturingRealtimePublisher();
        var publishedInsideHandler = false;

        string? boardId = null;
        var result = await RunAsync(
            new FakeRealtimeCommand { Topic = new RealtimeTopic("workspace", "Workspace", WorkspaceA1) },
            queue,
            CreateDataSession(context, tenant),
            publisher,
            _ =>
            {
                publishedInsideHandler = publisher.Calls.Count > 0;
                var board = Board.Create(
                    AccountA, WorkspaceA1, UserA, "Realtime Board", null, FixedTime);
                context.Boards.Add(board);
                boardId = board.Id.ToString();
                return Task.FromResult("created");
            });

        result.Should().Be("created");
        publishedInsideHandler.Should().BeFalse(
            "the publisher must not be reached while the handler is still inside the open transaction");

        publisher.Calls.Should().HaveCount(1, "a realtime command publishes exactly once");
        publisher.Calls[0].Topic.Should().Be(new RealtimeTopic("workspace", "Workspace", WorkspaceA1));
        publisher.Calls[0].Topic.Namespace.Should().Be("workspace");
        publisher.Calls[0].Payload.Should().Be("created");

        RealtimeChannelResolver.Workspace(WorkspaceA1).Should().Be($"workspace:{WorkspaceA1}");
        RealtimeChannelResolver.Board(WorkspaceA1, Guid.Parse(boardId!))
            .Should().Be($"workspace:{WorkspaceA1}:board:{boardId}");
        RealtimeChannelResolver.Item(WorkspaceA1, Guid.Parse(boardId!))
            .Should().Be($"workspace:{WorkspaceA1}:item:{boardId}");

        await using (var verifyContext = CreateContext(tenant))
        {
            var stored = await verifyContext.Boards.SingleAsync(b => b.Id == Guid.Parse(boardId!));
            stored.WorkspaceId.Should().Be(WorkspaceA1);
        }
    }

    [Fact]
    public async Task RealtimeCommand_HandlerFailure_RollsBackAndNeverPublishes()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seedContext = CreateContext(tenant))
        {
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA1, UserA, WorkspaceRole.Owner);
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserA);
        await using var context = CreateContext(tenant);
        var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
        var publisher = new CapturingRealtimePublisher();

        var act = () => RunAsync(
            new FakeRealtimeCommand { Topic = new RealtimeTopic("workspace", "Workspace", WorkspaceA1) },
            queue,
            CreateDataSession(context, tenant),
            publisher,
            _ =>
            {
                context.Boards.Add(Board.Create(
                    AccountA, WorkspaceA1, UserA, "Rolled Back Board", null, FixedTime));
                throw new InvalidOperationException("handler failed");
            });

        await act.Should().ThrowAsync<InvalidOperationException>();
        publisher.Calls.Should().BeEmpty(
            "a failed handler must never enqueue or publish");

        await using (var verifyContext = CreateContext(tenant))
        {
            (await verifyContext.Boards.CountAsync()).Should().Be(
                0, "the board created inside the failed handler must be rolled back");
        }
    }

    [Fact]
    public async Task RealtimeCommand_PublisherFailure_DoesNotCorruptResponse()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seedContext = CreateContext(tenant))
        {
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA1, UserA, WorkspaceRole.Owner);
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserA);
        await using var context = CreateContext(tenant);
        var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
        var publisher = new CapturingRealtimePublisher { ThrowOnPublish = new InvalidOperationException("transport down") };

        var result = await RunAsync(
            new FakeRealtimeCommand { Topic = new RealtimeTopic("workspace", "Workspace", WorkspaceA1) },
            queue,
            CreateDataSession(context, tenant),
            publisher,
            _ => Task.FromResult("created"));

        result.Should().Be("created",
            "a realtime transport failure must never corrupt the committed result (RULE.md §35)");
    }

    [Fact]
    public async Task NonRealtimeCommand_NeverPublishes()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seedContext = CreateContext(tenant))
        {
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA1, UserA, WorkspaceRole.Owner);
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserA);
        await using var context = CreateContext(tenant);
        var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
        var publisher = new CapturingRealtimePublisher();

        await RunPlainAsync(
            queue,
            CreateDataSession(context, tenant),
            publisher,
            _ => Task.FromResult("ok"));

        publisher.Calls.Should().BeEmpty(
            "commands without the IRealtimeRequest marker must not publish");
    }

    [Fact]
    public async Task WorkspaceMember_RealtimeCommand_Publishes()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seedContext = CreateContext(tenant))
        {
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA1, UserA, WorkspaceRole.Owner);
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserA);
        await using var context = CreateContext(tenant);
        var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
        var publisher = new CapturingRealtimePublisher();
        var permissionService = CreatePermissionService(context);
        var currentUser = new FakeCurrentUser { UserId = UserA };

        var result = await RunAsync(
            new AuthedRealtimeCommand { WorkspaceId = WorkspaceA1 },
            queue,
            CreateDataSession(context, tenant),
            publisher,
            _ => Task.FromResult("member-result"),
            authorizationStore: permissionService,
            currentUser: currentUser,
            tenant: tenant);

        result.Should().Be("member-result");
        publisher.Calls.Should().HaveCount(1,
            "a member with permission may publish after commit");
    }

    [Fact]
    public async Task NonMember_RealtimeCommand_DeniedBeforeHandler_NoPublish()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seedContext = CreateContext(tenant))
        {
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA1, UserA, WorkspaceRole.Owner);
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, OtherUser);
        await using var context = CreateContext(tenant);
        var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
        var publisher = new CapturingRealtimePublisher();
        var permissionService = CreatePermissionService(context);
        var currentUser = new FakeCurrentUser { UserId = OtherUser };

        var handlerExecuted = false;

        var act = () => RunAsync(
            new AuthedRealtimeCommand { WorkspaceId = WorkspaceA1 },
            queue,
            CreateDataSession(context, tenant),
            publisher,
            _ =>
            {
                handlerExecuted = true;
                return Task.FromResult("should-not-happen");
            },
            authorizationStore: permissionService,
            currentUser: currentUser,
            tenant: tenant);

        await act.Should().ThrowAsync<ForbiddenException>(
            "a user outside the workspace must be rejected");
        handlerExecuted.Should().BeFalse("denied before the handler");
        publisher.Calls.Should().BeEmpty("denied before any publish");
    }

    [Fact]
    public async Task RevokedMembership_RealtimeCommand_Denied_NoPublish()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seedContext = CreateContext(tenant))
        {
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA1, UserA, WorkspaceRole.Owner);
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserA);
        await using (var context = CreateContext(tenant))
        {
            var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
            var publisher = new CapturingRealtimePublisher();
            var permissionService = CreatePermissionService(context);
            var currentUser = new FakeCurrentUser { UserId = UserA };

            var first = await RunAsync(
                new AuthedRealtimeCommand { WorkspaceId = WorkspaceA1 },
                queue,
                CreateDataSession(context, tenant),
                publisher,
                _ => Task.FromResult("member-result"),
                authorizationStore: permissionService,
                currentUser: currentUser,
                tenant: tenant);

            first.Should().Be("member-result");
            publisher.Calls.Should().HaveCount(1, "member publishes once");
        }

        tenant.SetSystem();
        await using (var revokeContext = CreateContext(tenant))
        {
            var member = await revokeContext.WorkspaceMembers
                .SingleAsync(m => m.WorkspaceId == WorkspaceA1 && m.UserId == UserA);
            revokeContext.WorkspaceMembers.Remove(member);
            await revokeContext.SaveChangesAsync();
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserA);
        await using var context2 = CreateContext(tenant);
        var queue2 = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
        var publisher2 = new CapturingRealtimePublisher();
        var permissionService2 = CreatePermissionService(context2);
        var currentUser2 = new FakeCurrentUser { UserId = UserA };

        var handlerExecuted = false;

        var act = () => RunAsync(
            new AuthedRealtimeCommand { WorkspaceId = WorkspaceA1 },
            queue2,
            CreateDataSession(context2, tenant),
            publisher2,
            _ =>
            {
                handlerExecuted = true;
                return Task.FromResult("should-not-happen");
            },
            authorizationStore: permissionService2,
            currentUser: currentUser2,
            tenant: tenant);

        await act.Should().ThrowAsync<ForbiddenException>(
            "a revoked member must be rejected like any non-member");
        handlerExecuted.Should().BeFalse();
        publisher2.Calls.Should().BeEmpty("no publish after membership is revoked");
    }

    [Fact]
    public async Task WorkspaceQualifiedTopics_NeverLeakAcrossTenants()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seedContext = CreateContext(tenant))
        {
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA1, UserA, WorkspaceRole.Owner);
            await SeedWorkspaceAsync(seedContext, AccountA, WorkspaceA2, UserA, WorkspaceRole.Owner);
            await SeedWorkspaceAsync(seedContext, AccountB, WorkspaceB1, UserB, WorkspaceRole.Owner);
        }

        var published = new List<(RealtimeTopic Topic, object? Payload)>();

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserA);
        await using (var contextA1 = CreateContext(tenant))
        {
            var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
            var publisher = new CapturingRealtimePublisher();

            await RunAsync(
                new FakeRealtimeCommand { Topic = new RealtimeTopic("workspace", "Workspace", WorkspaceA1) },
                queue,
                CreateDataSession(contextA1, tenant),
                publisher,
                _ => Task.FromResult("ws1-data"));

            published.AddRange(publisher.Calls);
        }

        tenant.SetWorkspace(AccountB, WorkspaceB1, UserB);
        await using (var contextB1 = CreateContext(tenant))
        {
            var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
            var publisher = new CapturingRealtimePublisher();

            await RunAsync(
                new FakeRealtimeCommand { Topic = new RealtimeTopic("workspace", "Workspace", WorkspaceB1) },
                queue,
                CreateDataSession(contextB1, tenant),
                publisher,
                _ => Task.FromResult("wsB-data"));

            published.AddRange(publisher.Calls);
        }

        published.Should().HaveCount(2);

        var topicA1 = published[0].Topic;
        var topicB1 = published[1].Topic;

        topicA1.ResourceId.Should().Be(WorkspaceA1);
        topicB1.ResourceId.Should().Be(WorkspaceB1);
        topicA1.Should().NotBe(topicB1, "topics are tenant/workspace qualified and distinct");

        RealtimeChannelResolver.Workspace(WorkspaceA1).Should().Be($"workspace:{WorkspaceA1}");
        RealtimeChannelResolver.Workspace(WorkspaceB1).Should().Be($"workspace:{WorkspaceB1}");
        RealtimeChannelResolver.Workspace(WorkspaceA1)
            .Should().NotBe(RealtimeChannelResolver.Workspace(WorkspaceB1),
                "channel names must not collide across workspaces");

        published[0].Payload.Should().Be("ws1-data");
        published[1].Payload.Should().Be("wsB-data");
    }

    [Fact]
    public void ChannelNaming_AllChannelKinds_AreTenantQualified()
    {
        var pageId = Guid.Parse("A0000000-0000-0000-0000-00000000EE01");
        var userId = Guid.Parse("00000000-0000-0000-0000-0000000000FF");

        RealtimeChannelResolver.Workspace(WorkspaceA1).Should().Be($"workspace:{WorkspaceA1}");
        RealtimeChannelResolver.Board(WorkspaceA1, pageId).Should().Be($"workspace:{WorkspaceA1}:board:{pageId}");
        RealtimeChannelResolver.Item(WorkspaceA1, pageId).Should().Be($"workspace:{WorkspaceA1}:item:{pageId}");
        RealtimeChannelResolver.Page(WorkspaceA1, pageId).Should().Be($"workspace:{WorkspaceA1}:page:{pageId}");
        RealtimeChannelResolver.UserNotifications(userId).Should().Be($"user:{userId}:notifications");

        RealtimeChannelResolver.Board(WorkspaceA1, pageId)
            .Should().NotBe(RealtimeChannelResolver.Board(WorkspaceB1, pageId),
                "the same resource id in different workspaces must resolve to different channels");
    }
}
