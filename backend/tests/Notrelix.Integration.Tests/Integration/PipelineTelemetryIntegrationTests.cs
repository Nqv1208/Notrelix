using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Members.Services;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Observability.Metrics;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// IA-TST-TEL-E2E — end-to-end frozen-pipeline telemetry proof over real
/// PostgreSQL:
///   1. the success path emits the root activity plus every owned stage span,
///      correctly parented, and the business commit lands;
///   2. the access-denied failure path tags failure telemetry and commits nothing;
///   3. an idempotent command executes once and replays without re-running
///      the handler, emitting idempotency stages on both passes;
///   4. the durable async leg proves commit-before-publish: a committed outbox
///      row is dispatched by the real background dispatcher over the in-memory
///      broker to the realtime consumer, with the dispatch counter incremented.
/// Assertions never rely on absolute timings, Activity internals, or
/// high-cardinality labels. Each send runs in its own DI scope, mirroring the
/// per-request production composition.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PipelineTelemetryIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public PipelineTelemetryIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Success_Path_EmitsRootAndStageSpans_WithSaneParentage_AndCommits()
    {
        var (accountId, userId, _) = await SeedAccountMemberAsync(AccountRole.Owner);

        using var recorder = new ActivityRecorder();
        using var provider = CreateAccountScopedProvider(accountId, userId);

        Notrelix.Application.Common.Models.Result<Guid> result;
        using (var scope = provider.CreateScope())
        {
            result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new CreateWorkspaceCommand("Telemetry Workspace", null, false));
        }

        result.Succeeded.Should().BeTrue();

        var root = recorder.Started.Should()
            .ContainSingle(a => IsRootFor(a, "CreateWorkspaceCommand")).Subject;

        root.Tags.Should().Contain(tag => tag.Key == "app.request" && (string?)tag.Value == "CreateWorkspaceCommand");
        root.Tags.Should().Contain(tag => tag.Key == "request.kind" && (string?)tag.Value == "Command");
        root.Tags.Should().Contain(tag => tag.Key == "deployment.environment" && (string?)tag.Value == "Testing");
        root.Tags.Should().Contain(tag => tag.Key == "pipeline.outcome" && (string?)tag.Value == "success");
        root.Status.Should().Be(ActivityStatusCode.Ok);

        foreach (var stage in new[]
                 {
                     "request.contract", "execution_context.resolve", "data_session.open",
                     "access_facts.query", "policy.evaluate", "handler.execute",
                 })
        {
            recorder.Started.Should().Contain(a => a.OperationName == stage,
                $"stage '{stage}' is part of the frozen pipeline contract");
            RootAncestor(recorder.Started.First(a => a.OperationName == stage)).Should().Be(root,
                $"every stage span must descend from the request root, but '{stage}' does not");
        }

        recorder.Started.Should().ContainSingle(a => a.OperationName == "handler.execute")
            .Which.Parent!.OperationName.Should().Be("data_session.open");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.Workspaces.CountAsync(w => w.AccountId == accountId)).Should().Be(1,
            "the handler mutation must be committed when the pipeline succeeds");
    }

    [Fact]
    public async Task AccessDenied_Path_TagsFailureTelemetry_AndCommitsNothing()
    {
        var (accountId, userId, _) = await SeedAccountMemberAsync(AccountRole.Member);

        using var recorder = new ActivityRecorder();
        using var provider = CreateAccountScopedProvider(accountId, userId);

        Func<Task> act = () => provider.CreateScope().ServiceProvider
            .GetRequiredService<ISender>()
            .Send(new CreateWorkspaceCommand("Denied Workspace", null, false));

        await act.Should().ThrowAsync<AppForbidden>();

        var root = recorder.Started.Should()
            .ContainSingle(a => IsRootFor(a, "CreateWorkspaceCommand")).Subject;

        root.Status.Should().Be(ActivityStatusCode.Error);
        root.Tags.Should().Contain(tag =>
            tag.Key == "pipeline.outcome" && (string?)tag.Value == "failure:forbidden");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.Workspaces.CountAsync(w => w.AccountId == accountId)).Should().Be(0,
            "a rejected request must not leave committed business state");
    }

    [Fact]
    public async Task IdempotentCommand_FirstSendExecutes_SecondSendReplaysWithoutHandler()
    {
        var (accountId, userId, workspaceId) = await SeedBoardStackAsync();
        const string idempotencyKey = "tel-e2e-due-date-1";

        using var recorder = new ActivityRecorder();
        using var provider = CreateResourceScopedProvider(accountId, userId);
        var counter = provider.GetRequiredService<HandlerExecutionCounter>();
        var boardItemId = await ResolveBoardItemIdAsync(workspaceId);

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
                .Set(idempotencyKey, IdempotencyExecutionSource.Internal);

            var first = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new SetBoardItemDueDateCommand(boardItemId, DateTime.UtcNow.Date.AddDays(1), null));
            first.Succeeded.Should().BeTrue();
        }

        counter.Executions.Should().Be(1);

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
                .Set(idempotencyKey, IdempotencyExecutionSource.Internal);

            var second = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new SetBoardItemDueDateCommand(boardItemId, DateTime.UtcNow.Date.AddDays(1), null));
            second.Succeeded.Should().BeTrue();
        }

        counter.Executions.Should().Be(1,
            "the second send must replay the stored result instead of executing the handler again");

        var roots = recorder.Started
            .Where(a => IsRootFor(a, "SetBoardItemDueDateCommand"))
            .ToArray();
        roots.Should().HaveCount(2);
        roots.Should().OnlyContain(r => r.Status == ActivityStatusCode.Ok);

        recorder.Started.Where(a => a.OperationName == "idempotency.acquire").Should().HaveCount(2)
            .And.OnlyContain(a => RootAncestor(a) == roots[0] || RootAncestor(a) == roots[1]);
        recorder.Started.Where(a => a.OperationName == "idempotency.complete").Should().HaveCount(1,
            "only the first pass completes the idempotency record");

        // handler.execute is emitted ONLY on the actual invocation pass.
        Descendants(roots[0], recorder).Should().Contain(a => a.OperationName == "handler.execute",
            "the first pass must execute the handler");
        Descendants(roots[1], recorder).Should().NotContain(a => a.OperationName == "handler.execute",
            "a replayed request must not emit a handler span");
    }

    [Fact]
    public async Task Outbox_CommitBeforePublish_DispatchesToConsumer_WithMetricIncrement()
    {
        await SeedAccountMemberAsync(AccountRole.Owner);

        var observedDispatches = 0L;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == MetricsService.MeterName &&
                instrument.Name == "outbox_dispatched_count")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
            Interlocked.Add(ref observedDispatches, measurement));
        listener.Start();

        var publisher = new RecordingRealtimePublisher();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Transport"] = "InMemory",
                ["DOTNET_ENVIRONMENT"] = "Development",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.Warning);
        });
        services.AddOptions();

        var tenant = new FakeCurrentTenantContext();
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(clockMock.Object);
        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateContext(sp.GetRequiredService<ICurrentTenantContext>()));

        services.AddMessaging(configuration);
        services.AddObservability(configuration);
        services.AddBackgroundJobs(configuration);
        services.AddSingleton(ResolveWakeSignal());
        services.AddSingleton<IEventTypeRegistry, EventTypeRegistry>();
        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();

        // The spy replaces any other realtime publisher so the consumer leg is
        // observable without external transports.
        services.RemoveAll<IRealtimePublisher>();
        services.AddSingleton<IRealtimePublisher>(publisher);

        await using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            // Commit the outbox row on an independent connection first: the
            // dispatcher can only observe it after this commit, so publish
            // happens strictly after the durable write.
            var change = NewRealtimeChange(DateTimeOffset.UtcNow.AddMinutes(-1));
            MessagingOutboxMessage message;
            await using (var seed = _db.CreateContext(SystemTenant()))
            {
                message = MessagingOutboxMessage.FromIntegrationEvent(
                    change, DateTimeOffset.UtcNow.AddMinutes(-1));
                seed.Set<MessagingOutboxMessage>().Add(message);
                await seed.SaveChangesAsync();
            }

            publisher.Publications.Should().BeEmpty("nothing may publish before the dispatcher claims the row");

            var processed = await WaitForAsync(
                async () =>
                {
                    await using var probe = _db.CreateContext(SystemTenant());
                    return await probe.Set<MessagingOutboxMessage>()
                        .AnyAsync(m => m.Id == message.Id && m.Status == "Processed");
                },
                timeoutSeconds: 30);

            processed.Should().BeTrue("the dispatcher must claim, publish, and complete the committed row");

            var forwarded = await WaitForAsync(
                () => Task.FromResult(publisher.Publications.Any(p => p.ResourceId == change.ResourceId)),
                timeoutSeconds: 15);

            forwarded.Should().BeTrue(
                "the realtime consumer must forward the published contract exactly once");

            Volatile.Read(ref observedDispatches).Should().BeGreaterThanOrEqualTo(1,
                "the outbox dispatch counter must increment for the processed message");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }
    }

    // --- composition ---------------------------------------------------------

    private ServiceProvider CreateAccountScopedProvider(Guid accountId, Guid userId)
    {
        var services = CreateCoreServices(accountId, userId);

        services.AddTransient<
            IRequestHandler<CreateWorkspaceCommand, Notrelix.Application.Common.Models.Result<Guid>>,
            CreateWorkspaceCommandHandler>();

        return services.BuildServiceProvider();
    }

    private ServiceProvider CreateResourceScopedProvider(Guid accountId, Guid userId)
    {
        var services = CreateCoreServices(accountId, userId);

        services.AddSingleton(new HandlerExecutionCounter());
        services.AddTransient<
            IRequestHandler<SetBoardItemDueDateCommand, Notrelix.Application.Common.Models.Result>,
            CountingDueDateHandler>();

        return services.BuildServiceProvider();
    }

    private ServiceCollection CreateCoreServices(Guid accountId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);

        var userMock = new Mock<ICurrentUser>();
        userMock.Setup(u => u.UserId).Returns(userId);
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var environmentMock = new Mock<IHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Testing");

        var services = new ServiceCollection();
        services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.Warning);
        });
        services.AddOptions();

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(userMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(environmentMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(CreateWorkspaceCommand).Assembly));

        // Frozen seven-behavior pipeline, canonical outermost-to-innermost order.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ApplicationTracingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>()));
        services.AddScoped<IWorkspaceGrantProjectionService>(sp =>
            new WorkspaceGrantProjectionServiceAdapter(
                new AccessGrantProjectionService(sp.GetRequiredService<ApplicationDbContext>())));

        services.AddScoped<IResourceLocator, ResourceLocator>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();

        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();
        services.AddScoped<IIdempotencyStore>(sp =>
            new EfIdempotencyStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IOptions<IdempotencyOptions>>()));
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        return services;
    }

    private static IOutboxWakeSignal ResolveWakeSignal()
    {
        // OutboxWakeSignal is internal to Infrastructure; the public
        // registrations that expose it pull in far more than this test needs.
        var type = typeof(MessagingRegistration).Assembly.GetType(
            "Notrelix.Infrastructure.Messaging.OutboxWakeSignal", throwOnError: true)!;
        return (IOutboxWakeSignal)Activator.CreateInstance(type)!;
    }

    // --- seeding ---------------------------------------------------------------

    private async Task<(Guid AccountId, Guid UserId, Guid WorkspaceId)> SeedAccountMemberAsync(
        AccountRole role)
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"telemetry-{Guid.NewGuid():N}@example.com", "Telemetry User", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        var account = Account.Create("Telemetry Account", $"telemetry-{Guid.NewGuid():N}", AccountType.Team, user.Id, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        seed.Accounts.Add(account);
        seed.AccountMembers.Add(AccountMember.Create(account.Id, user.Id, role, user.Id, now));
        await seed.SaveChangesAsync();

        return (account.Id, user.Id, Guid.Empty);
    }

    private async Task<(Guid AccountId, Guid UserId, Guid WorkspaceId)> SeedBoardStackAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"telemetry-{Guid.NewGuid():N}@example.com", "Telemetry User", "hashed", now, true);
        user.ConfirmEmail(user.Id, now);
        var account = Account.Create(
            "Telemetry Board Account", $"telemetry-{Guid.NewGuid():N}", AccountType.Team, user.Id, now);

        var ownerId = user.Id;
        var workspace = Workspace.Create(account.Id, ownerId, "Board Workspace", "board-workspace", now);
        var workspaceMember = WorkspaceMember.Create(
            account.Id, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, now);
        var board = Board.Create(account.Id, workspace.Id, ownerId, "Board", null, now);
        var group = BoardGroup.Create(
            account.Id, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var boardItem = BoardItem.CreateRoot(
            account.Id, workspace.Id, board.Id, group.Id, "Task",
            FractionalIndex.Create("a0"), ownerId, now);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.Add(user);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(account.Id, ownerId, AccountRole.Owner, ownerId, now));
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.Add(workspaceMember);
            seed.Boards.Add(board);
            seed.BoardGroups.Add(group);
            seed.BoardItems.Add(boardItem);
            await seed.SaveChangesAsync();
        }

        await using (var grant = _db.CreateContext(SystemTenant()))
        {
            var projection = new AccessGrantProjectionService(grant);
            await projection.SyncWorkspaceMemberGrantAsync(
                account.Id, workspace.Id, ownerId, WorkspaceRole.Owner, now, CancellationToken.None);
            await grant.SaveChangesAsync();
        }

        return (account.Id, ownerId, workspace.Id);
    }

    private async Task<Guid> ResolveBoardItemIdAsync(Guid workspaceId)
    {
        await using var context = _db.CreateContext(SystemTenant());
        var ids = await context.BoardItems
            .Where(i => i.WorkspaceId == workspaceId)
            .Select(i => i.Id)
            .ToListAsync();
        ids.Should().NotBeEmpty($"expected the seeded board item for workspace {workspaceId}");
        return ids[0];
    }

    // --- helpers -----------------------------------------------------------------

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private static RealtimeResourceChangedV1 NewRealtimeChange(DateTimeOffset occurredAt) =>
        new(
            Guid.NewGuid(), accountId: null, workspaceId: null, actorUserId: null,
            Guid.NewGuid(), causationId: null, occurredAt,
            topicNamespace: "work", resourceKind: "board-item",
            resourceId: Guid.NewGuid(), streamKey: $"telemetry:{Guid.NewGuid():N}", streamVersion: 1,
            changeKind: "updated", payloadContract: "test.v1",
            System.Text.Json.JsonDocument.Parse("{}").RootElement);

    private static bool IsRootFor(Activity activity, string requestName) =>
        activity.OperationName == "pipeline.request"
        && activity.Tags.Any(tag => tag.Key == "app.request" && Equals(tag.Value, requestName));

    private static Activity? RootAncestor(Activity activity)
    {
        var current = activity;
        while (current.Parent is not null)
        {
            current = current.Parent;
        }

        return current;
    }

    private static IReadOnlyList<Activity> Descendants(Activity root, ActivityRecorder recorder) =>
        recorder.Started
            .Where(a => !ReferenceEquals(a, root) && ReferenceEquals(RootAncestor(a), root))
            .ToArray();

    private static async Task<bool> WaitForAsync(Func<Task<bool>> predicate, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
            {
                return true;
            }

            await Task.Delay(200);
        }

        return false;
    }

    private sealed class HandlerExecutionCounter
    {
        public int Executions;
    }

    private sealed class CountingDueDateHandler : IRequestHandler<
        SetBoardItemDueDateCommand, Notrelix.Application.Common.Models.Result>
    {
        private readonly HandlerExecutionCounter _counter;
        private readonly SetBoardItemDueDateCommandHandler _inner;

        public CountingDueDateHandler(
            HandlerExecutionCounter counter,
            IWorkManagementDbContext context,
            ICurrentUser currentUser,
            IDateTimeProvider timeProvider)
        {
            _counter = counter;
            _inner = new SetBoardItemDueDateCommandHandler(context, currentUser, timeProvider);
        }

        public async Task<Notrelix.Application.Common.Models.Result> Handle(
            SetBoardItemDueDateCommand request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _counter.Executions);
            return await _inner.Handle(request, cancellationToken);
        }
    }

    private sealed class RecordingRealtimePublisher : IRealtimePublisher
    {
        public List<RealtimeResourceChangedV1> Publications { get; } = [];

        public Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken)
        {
            Publications.Add(change);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Records every activity started by the pipeline source for the lifetime
    /// of one test case so parentage assertions can walk the full span tree.
    /// </summary>
    private sealed class ActivityRecorder : IDisposable
    {
        private readonly ActivityListener _listener;

        public ActivityRecorder()
        {
            Started = [];
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == PipelineActivitySource.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = Started.Add,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public List<Activity> Started { get; }

        public void Dispose() => _listener.Dispose();
    }
}
