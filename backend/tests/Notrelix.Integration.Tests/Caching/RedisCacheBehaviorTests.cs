using System.Threading;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Caching;
using Notrelix.Application.Common.Requests;
using Notrelix.Application.Common.Requests.Caching;
using Notrelix.Application.Common.Requests.Scoping;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Caching;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Governance.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Caching;

/// <summary>
/// FZ-INF-04 — cache provider certification against a real Redis server.
///
/// Expected resilience policy (documented for this certification):
/// - Authorized cache (AuthorizedCacheBehavior): Redis unavailable => fail closed.
///   The request fails; no response is ever served from an unreachable cache and
///   the handler still runs its authorization pipeline, so no data is served
///   without authorization.
/// - Ordinary/public query cache (PublicCacheBehavior): Redis unavailable =>
///   bypass the cache; the handler executes and the response is served, caching
///   is simply skipped.
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class RedisCacheBehaviorTests : IAsyncLifetime
{
    private static readonly Guid AccountA = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private static readonly Guid AccountB = Guid.Parse("B0000000-0000-0000-0000-000000000002");
    private static readonly Guid WorkspaceA1 = Guid.Parse("A0000000-0000-0000-0000-00000000AA01");
    private static readonly Guid WorkspaceA2 = Guid.Parse("A0000000-0000-0000-0000-00000000AA02");
    private static readonly Guid WorkspaceB1 = Guid.Parse("B0000000-0000-0000-0000-00000000BB01");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-0000000000AA");
    private static readonly DateTimeOffset FixedTime = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly CacheTestContainer _fixture;
    private readonly CacheKeyFactory _keyFactory = new(Options.Create(new CacheKeyOptions
    {
        Prefix = "notrelix",
        SchemaVersion = 1,
        Environment = "test",
    }));

    public RedisCacheBehaviorTests(CacheTestContainer fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private IRedisCacheService BuildCacheService(string connectionString)
    {
        var distributedCache = new RedisCache(new RedisCacheOptions
        {
            Configuration = connectionString,
            InstanceName = "Notrelix_",
        });

        var redisConfig = ConfigurationOptions.Parse(connectionString);
        redisConfig.AbortOnConnectFail = false;
        var multiplexer = ConnectionMultiplexer.Connect(redisConfig);

        return new RedisCacheService(distributedCache, multiplexer);
    }

    private IRedisCacheService BuildUnavailableCacheService()
    {
        // A port with no listener: AbortOnConnectFail=false + tiny timeouts make
        // every operation fail fast, deterministically, without touching real Redis.
        const string deadEndpoint = "localhost:6399,connectTimeout=1000,asyncTimeout=1000,syncTimeout=1000";
        return BuildCacheService(deadEndpoint);
    }

    private ApplicationDbContext CreateContext(ICurrentTenantContext tenant)
    {
        return _fixture.CreatePostgresContext(tenant);
    }

    private AuthorizedCacheBehavior<FakeAuthorizedRequest, string> BuildAuthorizedBehavior(
        IRedisCacheService cache,
        FakeCurrentTenantContext tenant,
        ApplicationDbContext context)
    {
        var permissionVersionProvider = new PermissionVersionProvider(context, NullLogger<PermissionVersionProvider>.Instance);
        return new AuthorizedCacheBehavior<FakeAuthorizedRequest, string>(
            cache, _keyFactory, tenant, permissionVersionProvider,
            NullLogger<AuthorizedCacheBehavior<FakeAuthorizedRequest, string>>.Instance);
    }

    [Fact]
    public async Task AccountScope_TenantPartitioned_NoLeakAcrossAccounts()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(AccountA, UserId);
        await using var context = CreateContext(tenant);
        var behavior = BuildAuthorizedBehavior(BuildCacheService(_fixture.RedisConnectionString), tenant, context);

        var request = new FakeAuthorizedRequest(AuthorizedCacheScope.Account, "account-data");

        var handlerCalls = 0;
        RequestHandlerDelegate<string> next = _ => { handlerCalls++; return Task.FromResult("value-A"); };

        var first = await behavior.Handle(request, next, CancellationToken.None);
        var second = await behavior.Handle(request, next, CancellationToken.None);

        first.Should().Be("value-A");
        second.Should().Be("value-A");
        handlerCalls.Should().Be(1, "the account-partitioned key hits on the second call");

        tenant.SetAccount(AccountB, UserId);
        var handlerCallsB = 0;
        RequestHandlerDelegate<string> nextB = _ => { handlerCallsB++; return Task.FromResult("value-B"); };

        var resultB = await behavior.Handle(request, nextB, CancellationToken.None);

        resultB.Should().Be("value-B", "account B must never see account A's cached value");
        handlerCallsB.Should().Be(1, "account B starts with its own key (miss)");
    }

    [Fact]
    public async Task WorkspaceScope_TenantPartitioned_NoLeakAcrossWorkspaces()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(AccountA, WorkspaceA1, UserId);
        await using var context = CreateContext(tenant);
        var behavior = BuildAuthorizedBehavior(BuildCacheService(_fixture.RedisConnectionString), tenant, context);

        var request = new FakeAuthorizedRequest(AuthorizedCacheScope.Workspace, "ws-data");

        var handlerCallsA1 = 0;
        RequestHandlerDelegate<string> nextA1 = _ => { handlerCallsA1++; return Task.FromResult("ws1"); };
        var first = await behavior.Handle(request, nextA1, CancellationToken.None);
        var second = await behavior.Handle(request, nextA1, CancellationToken.None);

        first.Should().Be("ws1");
        second.Should().Be("ws1");
        handlerCallsA1.Should().Be(1);

        tenant.SetWorkspace(AccountA, WorkspaceA2, UserId);
        var handlerCallsA2 = 0;
        RequestHandlerDelegate<string> nextA2 = _ => { handlerCallsA2++; return Task.FromResult("ws2"); };
        var resultA2 = await behavior.Handle(request, nextA2, CancellationToken.None);

        resultA2.Should().Be("ws2", "a sibling workspace must not see the cached result of another workspace");
        handlerCallsA2.Should().Be(1);
    }

    [Fact]
    public async Task PermissionedScope_CommitChangesEpoch_Invalidates()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var seedContext = CreateContext(tenant);

        var permission = ResourcePermission.Grant(
            AccountA, WorkspaceA1, ResourceKind.Create("work.board"),
            Guid.Parse("A0000000-0000-0000-0000-00000000AABB"),
            PermissionSubjectType.User, UserId,
            PermissionLevel.Editor, PermissionLevel.Owner,
            UserId, FixedTime);
        seedContext.ResourcePermissions.Add(permission);
        await seedContext.SaveChangesAsync();

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserId);
        await using var context = CreateContext(tenant);
        var behavior = BuildAuthorizedBehavior(BuildCacheService(_fixture.RedisConnectionString), tenant, context);
        var request = new FakeAuthorizedRequest(AuthorizedCacheScope.Permissioned, "perm-data");

        var handlerCalls = 0;
        RequestHandlerDelegate<string> next = _ => { handlerCalls++; return Task.FromResult("v1"); };
        var v1 = await behavior.Handle(request, next, CancellationToken.None);
        var hit = await behavior.Handle(request, next, CancellationToken.None);

        v1.Should().Be("v1");
        hit.Should().Be("v1");
        handlerCalls.Should().Be(1, "same permission epoch hits");

        tenant.SetSystem();
        await using var mutateContext = CreateContext(tenant);
        var stored = await mutateContext.ResourcePermissions.SingleAsync();
        stored.ChangeLevel(PermissionLevel.Manager, UserId, FixedTime.AddMinutes(5));
        await mutateContext.SaveChangesAsync();

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserId);
        await using var context2 = CreateContext(tenant);
        var behavior2 = BuildAuthorizedBehavior(BuildCacheService(_fixture.RedisConnectionString), tenant, context2);
        RequestHandlerDelegate<string> next2 = _ => { handlerCalls++; return Task.FromResult("v2"); };
        var afterCommit = await behavior2.Handle(request, next2, CancellationToken.None);

        afterCommit.Should().Be("v2", "a committed permission change advances the epoch and invalidates the old entry");
        handlerCalls.Should().Be(2);
    }

    [Fact]
    public async Task PermissionedScope_Rollback_DoesNotInvalidate()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var seedContext = CreateContext(tenant);

        var permission = ResourcePermission.Grant(
            AccountA, WorkspaceA1, ResourceKind.Create("work.board"),
            Guid.Parse("A0000000-0000-0000-0000-00000000AACC"),
            PermissionSubjectType.User, UserId,
            PermissionLevel.Editor, PermissionLevel.Owner,
            UserId, FixedTime);
        seedContext.ResourcePermissions.Add(permission);
        await seedContext.SaveChangesAsync();

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserId);
        await using var context = CreateContext(tenant);
        var behavior = BuildAuthorizedBehavior(BuildCacheService(_fixture.RedisConnectionString), tenant, context);
        var request = new FakeAuthorizedRequest(AuthorizedCacheScope.Permissioned, "perm-data");

        var handlerCalls = 0;
        RequestHandlerDelegate<string> next = _ => { handlerCalls++; return Task.FromResult("v1"); };
        await behavior.Handle(request, next, CancellationToken.None);
        handlerCalls.Should().Be(1);

        tenant.SetSystem();
        await using (var rollbackContext = CreateContext(tenant))
        {
            await using var transaction = await rollbackContext.Database.BeginTransactionAsync();
            var stored = await rollbackContext.ResourcePermissions.SingleAsync();
            stored.ChangeLevel(PermissionLevel.Manager, UserId, FixedTime.AddMinutes(5));
            await rollbackContext.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        tenant.SetWorkspace(AccountA, WorkspaceA1, UserId);
        await using var context2 = CreateContext(tenant);
        var behavior2 = BuildAuthorizedBehavior(BuildCacheService(_fixture.RedisConnectionString), tenant, context2);
        RequestHandlerDelegate<string> next2 = _ => { handlerCalls++; return Task.FromResult("v2"); };
        var afterRollback = await behavior2.Handle(request, next2, CancellationToken.None);

        afterRollback.Should().Be("v1",
            "a rolled-back permission change must not invalidate the cached authorization epoch");
        handlerCalls.Should().Be(1, "the cached value is still valid after rollback");
    }

    [Fact]
    public async Task PermissionedScope_Commit_InvalidatesExactlyOnce_AndEpochStabilizes()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var seedContext = CreateContext(tenant);

        var permission = ResourcePermission.Grant(
            AccountA, WorkspaceA1, ResourceKind.Create("work.board"),
            Guid.Parse("A0000000-0000-0000-0000-00000000AADD"),
            PermissionSubjectType.User, UserId,
            PermissionLevel.Editor, PermissionLevel.Owner,
            UserId, FixedTime);
        seedContext.ResourcePermissions.Add(permission);
        await seedContext.SaveChangesAsync();

        var provider = new PermissionVersionProvider(seedContext, NullLogger<PermissionVersionProvider>.Instance);
        var epochBefore = await provider.GetVersionAsync(AccountA, WorkspaceA1, UserId, CancellationToken.None);

        tenant.SetSystem();
        await using var mutateContext = CreateContext(tenant);
        var stored = await mutateContext.ResourcePermissions.SingleAsync();
        stored.ChangeLevel(PermissionLevel.Manager, UserId, FixedTime.AddMinutes(5));
        await mutateContext.SaveChangesAsync();

        var epochAfterCommit = await provider.GetVersionAsync(AccountA, WorkspaceA1, UserId, CancellationToken.None);
        epochAfterCommit.Should().NotBe(epochBefore, "one commit advances the epoch exactly once");

        var epochStable = await provider.GetVersionAsync(AccountA, WorkspaceA1, UserId, CancellationToken.None);
        epochStable.Should().Be(epochAfterCommit, "without further changes the epoch must not drift");
    }

    [Fact]
    public async Task AuthorizedBehavior_RedisUnavailable_FailsClosed()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(AccountA, UserId);
        await using var context = CreateContext(tenant);
        var behavior = BuildAuthorizedBehavior(BuildUnavailableCacheService(), tenant, context);

        var request = new FakeAuthorizedRequest(AuthorizedCacheScope.Account, "account-data");

        var handlerExecuted = false;
        RequestHandlerDelegate<string> next = _ => { handlerExecuted = true; return Task.FromResult("data"); };

        var act = () => behavior.Handle(request, next, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>(
            "an unavailable authorization cache must fail the request rather than serve unverified data");
        handlerExecuted.Should().BeFalse("fail closed: the handler must not execute when the cache check itself fails");
    }

    [Fact]
    public async Task PublicCacheBehavior_RedisUnavailable_BypassesCache()
    {
        var cache = BuildUnavailableCacheService();
        var behavior = new PublicCacheBehavior<FakePublicQuery, string>(
            cache, _keyFactory, NullLogger<PublicCacheBehavior<FakePublicQuery, string>>.Instance);

        var request = new FakePublicQuery("public-data");

        var handlerCalls = 0;
        RequestHandlerDelegate<string> next = _ => { handlerCalls++; return Task.FromResult("public-result"); };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be("public-result", "ordinary query cache must bypass when Redis is unavailable");
        handlerCalls.Should().Be(1);
    }

    private sealed record FakeAuthorizedRequest(
        AuthorizedCacheScope CacheScope,
        object CacheIdentity) : IAuthorizedCacheableRequest;

    private sealed record FakePublicQuery(object CacheIdentity)
        : IPublicCacheableQuery<string>, IGlobalRequest;
}
