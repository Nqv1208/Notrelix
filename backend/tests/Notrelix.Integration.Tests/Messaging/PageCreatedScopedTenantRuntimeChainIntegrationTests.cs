using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Abstractions;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Data.Projections.Activity;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Messaging;

/// <summary>
/// TAC-FRZ-018 tenant-envelope transport/runtime proof for the page-created
/// fact: CreatePage through the canonical pipeline -> page.created outbox ->
/// dispatcher -> MassTransit receive pipeline -> TenantContextConsumeFilter
/// -> dedup -> the registered consumer endpoint under the restored Workspace
/// tenant. This is NOT business-flow evidence: the registered
/// page-created consumer is a log-only stub, which DC-FLOW-07 excludes from
/// business evidence. Business proof for the page fact lives in the
/// producer-side outbox evidence and the pinning architecture gates.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PageCreatedScopedTenantRuntimeChainIntegrationTests : IAsyncLifetime
{
    private const string PageCreatedConsumerEndpoint = "notrelix-doc-page-created-v1";

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public PageCreatedScopedTenantRuntimeChainIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record PageGraph(Guid AccountId, Guid WorkspaceId, Guid AuthorId, string Title);

    private async Task<PageGraph> SeedWorkspaceAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var title = $"chain page {Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        var workspace = Workspace.Create(accountId, ownerId, "DC Page Chain WS", $"pagechain-{Guid.NewGuid():N}", now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(Domain.Workspaces.Members.WorkspaceMember.Create(
            accountId, workspace.Id, ownerId, Domain.Workspaces.Members.WorkspaceRole.Owner, ownerId, now));
        await seed.SaveChangesAsync();

        return new PageGraph(accountId, workspace.Id, ownerId, title);
    }

    [Fact]
    public async Task PageCreated_RunsThroughProductionDeliveryChain_UnderWorkspaceTenant()
    {
        var graph = await SeedWorkspaceAsync();
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
            var pageId = await CreatePageAsync(provider, graph);
            pageId.Should().NotBeEmpty();

            var outbox = await WaitForOutboxAsync(graph);
            outbox.Should().NotBeNull();
            outboxEventId = outbox!.EventId;

            outbox.AccountId.Should().Be(graph.AccountId);
            outbox.WorkspaceId.Should().Be(graph.WorkspaceId);
            outbox.MessageName.Should().Be("page.created");
            outbox.SchemaVersion.Should().Be(1);
            outbox.PayloadJson.RootElement.GetProperty("workspaceId").GetGuid().Should().Be(graph.WorkspaceId);

            var dedupCompleted = await WaitForDedupSucceededAsync(outboxEventId, PageCreatedConsumerEndpoint);
            dedupCompleted.Should().BeTrue();

            var dispatcherCompleted = await WaitForOutboxProcessedAsync(outbox.Id);
            dispatcherCompleted.Should().BeTrue();
        }
        finally
        {
            foreach (var hosted in hostedServices.Reverse())
            {
                await hosted.StopAsync(CancellationToken.None);
            }
        }

        recorder.ObservedWorkspaceSet.Should().BeTrue(
            "TenantContextConsumeFilter must restore the Workspace tenant before the consumer pipe runs");
        recorder.LastWorkspaceAccountId.Should().Be(graph.AccountId);
        recorder.LastWorkspaceId.Should().Be(graph.WorkspaceId);
        recorder.LastWorkspaceIsSystem.Should().BeFalse(
            "a Workspace-scoped integration event must not execute its consumer as System");
        recorder.ClearedAfterWorkspace.Should().BeTrue(
            "TenantContextConsumeFilter must clear tenant state after consume completion");
    }

    private static async Task<Guid> CreatePageAsync(ServiceProvider provider, PageGraph graph)
    {
        await using var scope = provider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<Notrelix.Application.Common.Context.IExecutionContextAccessor>()
            .SetUser(graph.AuthorId, "chain-author@example.com", "Chain Author");

        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreatePageCommand(graph.WorkspaceId, graph.Title, null), CancellationToken.None);

        ((Result<Guid>)result).Succeeded.Should().BeTrue();
        return ((Result<Guid>)result).Data;
    }

    private ServiceProvider BuildProvider(TenantObservationRecorder recorder, PageGraph graph)
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

        builder.Services.AddScoped<IActivityProjectionDbContext>(sp =>
            new ActivityProjectionTenantProbe(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<ICurrentTenantContext>(),
                recorder));

        return builder.Services.BuildServiceProvider();
    }

    private async Task<MessagingOutboxMessage?> WaitForOutboxAsync(PageGraph graph)
    {
        MessagingOutboxMessage? found = null;
        var completed = await WaitForAsync(async () =>
        {
            await using var probe = _db.CreateContext(SystemTenant());
            found = await probe.Set<MessagingOutboxMessage>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.MessageName == "page.created"
                    && m.AccountId == graph.AccountId
                    && m.WorkspaceId == graph.WorkspaceId);
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

        public void Reset()
        {
            lock (_gate)
            {
                ObservedWorkspaceSet = false;
                LastWorkspaceAccountId = null;
                LastWorkspaceId = null;
                LastWorkspaceIsSystem = false;
                ClearedAfterWorkspace = false;
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
            return _inner.SaveChangesAsync(cancellationToken);
        }
    }
}
