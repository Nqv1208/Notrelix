using System.Collections.Concurrent;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Projections.Activity;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Observability.Metrics;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// TAC-DC-005 — proves the required production runtime owner chain for the
/// comment fact: CreateComment through the canonical pipeline -> comment.created
/// outbox -> dispatcher -> MassTransit receive pipeline ->
/// TenantContextConsumeFilter -> DeduplicationConsumeFilter -> real
/// CommentCreatedActivityConsumer -> WorkspaceActivityLogRecord projection
/// under the expected Workspace tenant.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class CommentCreatedScopedTenantRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string ActivityConsumerEndpoint = "notrelix-activity-comment-created-v1";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CommentCreatedScopedTenantRuntimeChainIntegrationTests(PostgresTestContainer db)
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
    public async Task CommentCreated_RunsThroughProductionDeliveryChainAndProjectsUnderWorkspaceTenant()
    {
        var graph = await SeedBoardItemStackAsync();
        var recorder = new TenantObservationRecorder();
        await using var provider = BuildProvider(recorder, graph);

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        Guid outboxEventId;
        try
        {
            // Reset BEFORE the mutation: once the commit lands, the background
            // dispatcher may consume the fact at any moment, and resetting the
            // recorder afterwards would race against real delivery evidence.
            recorder.Reset();
            await CreateCommentAsync(provider, graph);

            var outbox = await WaitForOutboxAsync(graph);
            outbox.Should().NotBeNull();
            outboxEventId = outbox!.EventId;

            outbox.AccountId.Should().Be(graph.AccountId);
            outbox.WorkspaceId.Should().Be(graph.WorkspaceId);
            outbox.MessageName.Should().Be("comment.created");
            outbox.SchemaVersion.Should().Be(1);
            outbox.PayloadJson.RootElement.GetProperty("targetType").GetString().Should().Be(
                "work-management.board-item",
                "the producer maps the canonical resource kind string");
            outbox.PayloadJson.RootElement.GetProperty("body").GetString().Should().Be(
                graph.Content,
                "the producer maps the owned comment content fact");

            var activity = await WaitForActivityAsync(outboxEventId, graph.WorkspaceId);
            activity.Should().NotBeNull();
            activity!.TargetId.Should().Be(graph.ItemId);
            activity.TargetType.Should().Be("work-management.board-item");

            var dispatcherCompleted = await WaitForOutboxProcessedAsync(outbox.Id);
            dispatcherCompleted.Should().BeTrue();

            var dedupCompleted = await WaitForDedupSucceededAsync(outboxEventId, ActivityConsumerEndpoint);
            dedupCompleted.Should().BeTrue();

            // Duplicate delivery of the SAME business event: the exact outward
            // event is deserialized from the committed outbox with the
            // production serializer and republished with the same business
            // EventId — the dedup identity of the consume pipeline. The proof
            // observes the second real claim attempt reaching the dedup
            // filter, not a hoped-for delivery.
            var duplicate = DeserializeOutboxEvent(outbox);
            await provider.GetRequiredService<IIntegrationEventBus>()
                .PublishAsync(duplicate);
            (await WaitForSecondClaimAsync(outboxEventId, ActivityConsumerEndpoint)).Should().BeTrue(
                "the duplicate delivery must actually reach the DeduplicationConsumeFilter and attempt a second claim");

            await using var midProbe = _db.CreateContext(SystemTenant());
            (await midProbe.Set<WorkspaceActivityLogRecord>()
                .IgnoreQueryFilters()
                .CountAsync(a => a.SourceEventId == outboxEventId)).Should().Be(1,
                "redelivering the same business event must not duplicate the logical activity");
            (await midProbe.Set<MessagingProcessedEvent>()
                .IgnoreQueryFilters()
                .CountAsync(p => p.EventId == outboxEventId
                    && p.ConsumerName == ActivityConsumerEndpoint
                    && p.Status == "Succeeded")).Should().Be(1,
                "the dedup identity stays coherent: one succeeded delivery, the duplicate skipped");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        await using var probe = _db.CreateContext(SystemTenant());
        (await probe.Set<WorkspaceActivityLogRecord>()
            .IgnoreQueryFilters()
            .CountAsync(a => a.SourceEventId == outboxEventId)).Should().Be(1,
            "the real production consumer must project the committed comment fact exactly once");

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "TenantContextConsumeFilter must restore the Workspace tenant before the consumer pipe runs");
        recorder.LastWorkspaceAccountId.Should().Be(graph.AccountId);
        recorder.LastWorkspaceId.Should().Be(graph.WorkspaceId);
        recorder.LastWorkspaceIsSystem.Should().BeFalse();
        recorder.SaveChangesWorkspaceId.Should().Be(graph.WorkspaceId,
            "the real activity consumer must persist while the scoped tenant context is Workspace-scoped");
        recorder.SaveChangesIsSystem.Should().BeFalse(
            "a Workspace-scoped integration event must not execute its consumer as System");
        recorder.ClearedAfterWorkspace.Should().BeTrue(
            "TenantContextConsumeFilter must clear tenant state after consume completion");
    }

    /// <summary>
    /// M7 freeze — a successful reply is the same canonical outward fact:
    /// CommentReplyCreatedDomainEvent maps onto comment.created with the
    /// exact ParentCommentId, and the activity consumer projects it like any
    /// created comment. Root and reply both reach the stream.
    /// </summary>
    [Fact]
    public async Task CommentReply_MapsOntoCanonicalCommentCreated_WithExactParentIdentity()
    {
        var graph = await SeedBoardItemStackAsync();
        await using var provider = BuildProvider(new TenantObservationRecorder(), graph);

        var hostedServices = provider.GetServices<IHostedService>().ToArray();
        foreach (var hosted in hostedServices)
        {
            await hosted.StartAsync(CancellationToken.None);
        }

        try
        {
            Guid rootCommentId;
            await using (var scope = provider.CreateAsyncScope())
            {
                scope.ServiceProvider.GetRequiredService<Notrelix.Application.Common.Context.IExecutionContextAccessor>()
                    .SetUser(graph.AuthorId, "chain-author@example.com", "Chain Author");
                var root = await scope.ServiceProvider.GetRequiredService<ISender>()
                    .Send(CreateCommentCommand.ForBoardItem(graph.ItemId, graph.Content, null), CancellationToken.None);
                ((Result<Guid>)root).Succeeded.Should().BeTrue();
                rootCommentId = ((Result<Guid>)root).Data;
            }

            var replyContent = $"reply content {Guid.NewGuid():N}";
            await using (var scope = provider.CreateAsyncScope())
            {
                scope.ServiceProvider.GetRequiredService<Notrelix.Application.Common.Context.IExecutionContextAccessor>()
                    .SetUser(graph.AuthorId, "chain-author@example.com", "Chain Author");
                var reply = await scope.ServiceProvider.GetRequiredService<ISender>()
                    .Send(CreateCommentCommand.ForBoardItem(graph.ItemId, replyContent, rootCommentId), CancellationToken.None);
                ((Result<Guid>)reply).Succeeded.Should().BeTrue();
            }

            var rootOutbox = await WaitForCommentOutboxWithParentAsync(graph, null);
            var replyOutbox = await WaitForCommentOutboxWithParentAsync(graph, rootCommentId);
            rootOutbox.Should().NotBeNull("the root comment stages the canonical outward fact");
            replyOutbox.Should().NotBeNull("the reply stages the same canonical outward fact with its parent identity");

            rootOutbox!.PayloadJson.RootElement.GetProperty("parentCommentId").ValueKind
                .Should().Be(JsonValueKind.Null, "the root comment carries no parent identity");
            replyOutbox!.PayloadJson.RootElement.GetProperty("parentCommentId").GetGuid()
                .Should().Be(rootCommentId);

            (await WaitForActivityAsync(rootOutbox.EventId, graph.WorkspaceId)).Should().NotBeNull();
            (await WaitForActivityAsync(replyOutbox.EventId, graph.WorkspaceId)).Should().NotBeNull(
                "the reply reaches the activity projection like any created comment");
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }
    }

    private async Task<MessagingOutboxMessage?> WaitForCommentOutboxWithParentAsync(
        CommentGraph graph,
        Guid? parentCommentId)
    {
        MessagingOutboxMessage? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            var candidates = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .Where(m => m.MessageName == "comment.created"
                    && m.AccountId == graph.AccountId
                    && m.WorkspaceId == graph.WorkspaceId)
                .ToListAsync();
            found = candidates.FirstOrDefault(m =>
                (parentCommentId is null
                    ? m.PayloadJson.RootElement.TryGetProperty("parentCommentId", out var pc)
                        && pc.ValueKind == JsonValueKind.Null
                    : m.PayloadJson.RootElement.TryGetProperty("parentCommentId", out var pr)
                        && pr.GetGuid() == parentCommentId.Value));
            return found is not null;
        });

        return completed ? found : null;
    }

    /// <summary>
    /// Deserializes the committed outbox payload with the production
    /// serializer exactly as the OutboxDispatcher does (CamelCase naming plus
    /// the (messageName, schemaVersion) catalog identity) — never a
    /// hand-built copy of the event.
    /// </summary>
    private static IIntegrationEvent DeserializeOutboxEvent(MessagingOutboxMessage message)
    {
        var catalog = new IntegrationEventCatalog();
        var eventType = catalog.Resolve(new EventContractKey(message.MessageName, message.SchemaVersion));
        return message.PayloadJson.Deserialize(eventType, OutboxSerializerOptions)
            .Should().BeAssignableTo<IIntegrationEvent>().Which;
    }

    private static readonly JsonSerializerOptions OutboxSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Waits until the dedup store records a SECOND claim attempt for the
    /// (eventId, consumer) identity — the observable signature of a real
    /// duplicate delivery reaching the DeduplicationConsumeFilter.
    /// </summary>
    private static async Task<bool> WaitForSecondClaimAsync(Guid eventId, string consumerName, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (RecordingDeduplicationStore.ClaimAttempts(eventId, consumerName) >= 2)
            {
                return true;
            }

            await Task.Delay(200);
        }

        return false;
    }

    private sealed record CommentGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid AuthorId,
        Guid ItemId,
        string Content);

    private async Task<CommentGraph> SeedBoardItemStackAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var content = $"chain content {Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(accountId, ownerId, "DC Chain WS", $"chain-{Guid.NewGuid():N}", now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "DC Chain Board", null, now);
        var group = BoardGroup.Create(
            accountId, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, now);
        var item = BoardItem.CreateRoot(
            accountId, workspace.Id, board.Id, group.Id, "Task",
            FractionalIndex.Create("a0"), ownerId, now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(Domain.Workspaces.Members.WorkspaceMember.Create(
            accountId, workspace.Id, ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner, ownerId, now));
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        return new CommentGraph(accountId, workspace.Id, ownerId, item.Id, content);
    }

    private static async Task CreateCommentAsync(ServiceProvider provider, CommentGraph graph)
    {
        await using var scope = provider.CreateAsyncScope();

        // The host boundary (HttpRequestContextMiddleware in production) seeds
        // the authenticated actor into the request execution context before
        // the MediatR pipeline resolves it.
        scope.ServiceProvider.GetRequiredService<Notrelix.Application.Common.Context.IExecutionContextAccessor>()
            .SetUser(graph.AuthorId, "chain-author@example.com", "Chain Author");

        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(CreateCommentCommand.ForBoardItem(graph.ItemId, graph.Content, null), CancellationToken.None);

        ((Result<Guid>)result).Succeeded.Should().BeTrue();
    }

    private ServiceProvider BuildProvider(TenantObservationRecorder recorder, CommentGraph graph)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
                ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
                ["Messaging:Transport"] = "InMemory",
                ["Rls:Enabled"] = "true",
                ["Rls:SetSessionContext"] = "true",
                ["DOTNET_ENVIRONMENT"] = "Testing",
                ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "notrelix-test",
                ["JwtSettings:Audience"] = "notrelix-test",
                ["JwtSettings:ExpireMinutes"] = "30",
                ["JwtSettings:RefreshTokenExpireDays"] = "7",
            })
            .Build();

        var builder = new HostApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = _db.ConnectionString,
            ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false",
            ["Messaging:Transport"] = "InMemory",
            ["Rls:Enabled"] = "true",
            ["Rls:SetSessionContext"] = "true",
            ["DOTNET_ENVIRONMENT"] = "Testing",
            ["JwtSettings:SecretKey"] = "test-secret-key-at-least-32-characters-long",
            ["JwtSettings:Issuer"] = "notrelix-test",
            ["JwtSettings:Audience"] = "notrelix-test",
            ["JwtSettings:ExpireMinutes"] = "30",
            ["JwtSettings:RefreshTokenExpireDays"] = "7",
        });

        builder.Services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        builder.Services.AddSingleton(TimeProvider.System);

        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Testing");
        builder.Services.AddSingleton(environment.Object);

        // Full production composition, then host-boundary overrides AFTER it so
        // they win in the resolved graph — same approach as the registration
        // runtime-chain evidence.
        builder.Services.AddInfrastructure(configuration, environment.Object);

        builder.Services.AddScoped<ICurrentTenantContext>(_ =>
            new RecordingCurrentTenantContext(new CurrentTenantContext(), recorder));
        builder.Services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser
        {
            UserId = graph.AuthorId,
            Email = "chain-author@example.com",
            Name = "Chain Author",
        });

        builder.Services.AddScoped<IIntegrationEventCollector, IntegrationEventCollector>();
        builder.AddApplicationServices();

        // Wrap the production dedup store with a recording decorator so the
        // duplicate-delivery proof observes real claim attempts instead of
        // assuming a delayed publish reached the filter. Scoped like the
        // production registration — the store's ApplicationDbContext is
        // tenant-scoped and must not become a captive singleton dependency.
        builder.Services.Replace(
            ServiceDescriptor.Scoped<IMessageDeduplicationStore>(sp =>
                new RecordingDeduplicationStore(
                    new MessageDeduplicationStore(
                        sp.GetRequiredService<ApplicationDbContext>(),
                        sp.GetRequiredService<IDateTimeProvider>(),
                        sp.GetRequiredService<MetricsService>()))));

        builder.Services.AddScoped<IActivityProjectionDbContext>(sp =>
            new ActivityProjectionTenantProbe(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<ICurrentTenantContext>(),
                recorder));

        return builder.Services.BuildServiceProvider();
    }

    private async Task<MessagingOutboxMessage?> WaitForOutboxAsync(CommentGraph graph)
    {
        MessagingOutboxMessage? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.MessageName == "comment.created"
                    && m.AccountId == graph.AccountId
                    && m.WorkspaceId == graph.WorkspaceId);
            return found is not null;
        });

        return completed ? found : null;
    }

    private async Task<WorkspaceActivityLogRecord?> WaitForActivityAsync(Guid sourceEventId, Guid workspaceId)
    {
        WorkspaceActivityLogRecord? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<WorkspaceActivityLogRecord>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.SourceEventId == sourceEventId && a.WorkspaceId == workspaceId);
            return found is not null;
        });

        return completed ? found : null;
    }

    private async Task<bool> WaitForOutboxProcessedAsync(Guid outboxId)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .AnyAsync(m => m.Id == outboxId && m.Status == "Processed");
        });
    }

    private async Task<bool> WaitForDedupSucceededAsync(Guid eventId, string consumerName)
    {
        return await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            return await probe.Set<MessagingProcessedEvent>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.EventId == eventId
                    && p.ConsumerName == consumerName
                    && p.Status == "Succeeded");
        });
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> predicate, int timeoutSeconds = 30)
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

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class TenantObservationRecorder
    {
        private readonly object _gate = new();

        public bool ObservedWorkspaceSet { get; private set; }
        public Guid? LastWorkspaceAccountId { get; private set; }
        public Guid? LastWorkspaceId { get; private set; }
        public bool LastWorkspaceIsSystem { get; private set; }
        public bool ClearedAfterWorkspace { get; private set; }

        public Guid? SaveChangesWorkspaceId { get; private set; }
        public bool SaveChangesIsSystem { get; private set; }

        public void RecordWorkspace(Guid accountId, Guid workspaceId, bool isSystemContext)
        {
            lock (_gate)
            {
                ObservedWorkspaceSet = true;
                LastWorkspaceAccountId = accountId;
                LastWorkspaceId = workspaceId;
                LastWorkspaceIsSystem = isSystemContext;
            }
        }

        public void RecordClear()
        {
            lock (_gate)
            {
                if (ObservedWorkspaceSet)
                {
                    ClearedAfterWorkspace = true;
                }
            }
        }

        public void RecordSaveChanges(Guid? accountId, Guid? workspaceId, bool isSystemContext)
        {
            lock (_gate)
            {
                SaveChangesWorkspaceId = workspaceId;
                SaveChangesIsSystem = isSystemContext;
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                ObservedWorkspaceSet = false;
                LastWorkspaceAccountId = null;
                LastWorkspaceId = null;
                LastWorkspaceIsSystem = false;
                ClearedAfterWorkspace = false;
                SaveChangesWorkspaceId = null;
                SaveChangesIsSystem = false;
            }
        }
    }

    private sealed class RecordingCurrentTenantContext : ICurrentTenantContext
    {
        private readonly CurrentTenantContext _inner;
        private readonly TenantObservationRecorder _recorder;

        public RecordingCurrentTenantContext(CurrentTenantContext inner, TenantObservationRecorder recorder)
        {
            _inner = inner;
            _recorder = recorder;
        }

        public Guid? AccountId => _inner.AccountId;
        public Guid? WorkspaceId => _inner.WorkspaceId;
        public Guid? UserId => _inner.UserId;
        public bool IsSystemContext => _inner.IsSystemContext;
        public bool IsResolved => _inner.IsResolved;

        public Guid RequireAccountId() => _inner.RequireAccountId();
        public Guid RequireWorkspaceId() => _inner.RequireWorkspaceId();
        public Guid RequireUserId() => _inner.RequireUserId();

        public void SetUser(Guid userId) => _inner.SetUser(userId);
        public void SetAccountHint(Guid accountId) => _inner.SetAccountHint(accountId);
        public void SetAccount(Guid accountId, Guid? userId) => _inner.SetAccount(accountId, userId);

        public void SetWorkspace(Guid accountId, Guid workspaceId, Guid? userId)
        {
            _inner.SetWorkspace(accountId, workspaceId, userId);
            _recorder.RecordWorkspace(accountId, workspaceId, _inner.IsSystemContext);
        }

        public void SetSystem() => _inner.SetSystem();

        public void Clear()
        {
            _inner.Clear();
            _recorder.RecordClear();
        }
    }

    private sealed class ActivityProjectionTenantProbe : IActivityProjectionDbContext
    {
        private readonly ApplicationDbContext _inner;
        private readonly ICurrentTenantContext _tenant;
        private readonly TenantObservationRecorder _recorder;

        public ActivityProjectionTenantProbe(
            ApplicationDbContext inner,
            ICurrentTenantContext tenant,
            TenantObservationRecorder recorder)
        {
            _inner = inner;
            _tenant = tenant;
            _recorder = recorder;
        }

        public DbSet<WorkspaceActivityLogRecord> WorkspaceActivityLogs => _inner.WorkspaceActivityLogs;
        public DbSet<ActivityReadStateRecord> ActivityReadStates => _inner.ActivityReadStates;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            _recorder.RecordSaveChanges(_tenant.AccountId, _tenant.WorkspaceId, _tenant.IsSystemContext);
            return _inner.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Full-delegating decorator around the production dedup store that records
    /// claim attempts per (eventId, consumer) so the duplicate-delivery proof can
    /// observe that a redelivery actually reached the filter — no sleeping, no
    /// hoping. The static registry is per-test-class keyed by identity and the
    /// decorator never changes the store's own decisions.
    /// </summary>
    internal sealed class RecordingDeduplicationStore(IMessageDeduplicationStore inner) : IMessageDeduplicationStore
    {
        private static readonly ConcurrentDictionary<(Guid EventId, string ConsumerName), int> Claims = new();

        public static int ClaimAttempts(Guid eventId, string consumerName) =>
            Claims.TryGetValue((eventId, consumerName), out var count) ? count : 0;

        public static void Reset() => Claims.Clear();

        public Task<bool> IsProcessedAsync(Guid messageId, string consumerName, CancellationToken cancellationToken) =>
            inner.IsProcessedAsync(messageId, consumerName, cancellationToken);

        public async Task<bool> TryClaimProcessingAsync(
            Guid messageId,
            string consumerName,
            string messageName,
            int messageVersion,
            Guid? sourceEventId,
            Guid? workspaceId,
            CancellationToken cancellationToken)
        {
            Claims.AddOrUpdate((messageId, consumerName), 1, (_, count) => count + 1);
            return await inner.TryClaimProcessingAsync(
                messageId, consumerName, messageName, messageVersion, sourceEventId, workspaceId, cancellationToken);
        }

        public void MarkSucceeded(Guid messageId, string consumerName, DateTimeOffset processedAt) =>
            inner.MarkSucceeded(messageId, consumerName, processedAt);
    }

}
