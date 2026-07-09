using Microsoft.Extensions.Options;

namespace Notrelix.Application.Tests.Common.Caching;

public class AuthorizedCacheBehaviorTests
{
    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid WorkspaceId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly string PermissionVersion = "perm:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb:cccccccc-cccc-cccc-cccc-cccccccccccc:638000000000000000";

    #region Test records

    public sealed record AccountScopedQuery : IRequest<string>, IAuthorizedCacheableRequest
    {
        public AuthorizedCacheScope CacheScope => AuthorizedCacheScope.Account;
        public object CacheIdentity => new { Type = "account" };
    }

    public sealed record WorkspaceScopedQuery : IRequest<string>, IAuthorizedCacheableRequest
    {
        public AuthorizedCacheScope CacheScope => AuthorizedCacheScope.Workspace;
        public object CacheIdentity => new { Type = "workspace" };
    }

    public sealed record UserScopedQuery : IRequest<string>, IAuthorizedCacheableRequest
    {
        public AuthorizedCacheScope CacheScope => AuthorizedCacheScope.User;
        public object CacheIdentity => new { Type = "user" };
    }

    public sealed record PermissionedScopedQuery : IRequest<string>, IAuthorizedCacheableRequest
    {
        public AuthorizedCacheScope CacheScope => AuthorizedCacheScope.Permissioned;
        public object CacheIdentity => new { Type = "permissioned" };
        public TimeSpan? CacheTtl => TimeSpan.FromMinutes(1);
    }

    public sealed record NonCacheableQuery : IRequest<string>;

    public sealed record UnknownScopeQuery : IRequest<string>, IAuthorizedCacheableRequest
    {
        public AuthorizedCacheScope CacheScope => (AuthorizedCacheScope)999;
        public object CacheIdentity => new { Type = "unknown" };
    }

    #endregion

    #region Helpers

    private static Mock<IRedisCacheService> CreateCacheService()
    {
        var mock = new Mock<IRedisCacheService>();
        mock.Setup(x => x.GetAsync<string>(It.IsAny<string>()))
            .ReturnsAsync((string?)null);
        return mock;
    }

    private static CacheKeyFactory CreateKeyFactory()
    {
        var options = Options.Create(new CacheKeyOptions
        {
            Environment = "test",
            Prefix = "notrelix",
            SchemaVersion = 1
        });
        return new CacheKeyFactory(options);
    }

    private static Mock<ICurrentTenantContext> CreateTenantContext()
    {
        var mock = new Mock<ICurrentTenantContext>();
        mock.Setup(x => x.RequireAccountId()).Returns(AccountId);
        mock.Setup(x => x.RequireWorkspaceId()).Returns(WorkspaceId);
        mock.Setup(x => x.RequireUserId()).Returns(UserId);
        return mock;
    }

    private static Mock<IPermissionVersionProvider> CreateVersionProvider(string? version = null)
    {
        var mock = new Mock<IPermissionVersionProvider>();
        mock.Setup(x => x.GetVersionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(version ?? PermissionVersion);
        return mock;
    }

    private static AuthorizedCacheBehavior<TRequest, string> CreateBehavior<TRequest>(
        Mock<IRedisCacheService>? cache = null,
        CacheKeyFactory? keyFactory = null,
        Mock<ICurrentTenantContext>? tenant = null,
        Mock<IPermissionVersionProvider>? versionProvider = null)
        where TRequest : notnull
    {
        return new AuthorizedCacheBehavior<TRequest, string>(
            cache?.Object ?? CreateCacheService().Object,
            keyFactory ?? CreateKeyFactory(),
            tenant?.Object ?? CreateTenantContext().Object,
            versionProvider?.Object ?? CreateVersionProvider().Object,
            Mock.Of<ILogger<AuthorizedCacheBehavior<TRequest, string>>>());
    }

    #endregion

    // ========================================================================
    // Phase 1: Permissioned Cache Fix
    // ========================================================================

    [Fact]
    public async Task PermissionedScope_UsesPermissionVersionProvider_NotDefault()
    {
        var provider = new Mock<IPermissionVersionProvider>();
        provider.Setup(x => x.GetVersionAsync(AccountId, WorkspaceId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("custom-version-2");

        var cache = CreateCacheService();
        var behavior = CreateBehavior<PermissionedScopedQuery>(cache: cache, versionProvider: provider);
        var handlerInvoked = false;

        var response = await behavior.Handle(
            new PermissionedScopedQuery(),
            _ => { handlerInvoked = true; return Task.FromResult("response"); },
            default);

        provider.Verify(x => x.GetVersionAsync(AccountId, WorkspaceId, UserId, It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.GetAsync<string>(It.Is<string>(k => k.Contains("custom-version-2"))), Times.Once);
        handlerInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task PermissionedScope_WithVersionInKey_UsesProvidedVersion()
    {
        var factory = CreateKeyFactory();
        var key = factory.Permissioned(AccountId, WorkspaceId, UserId, PermissionVersion,
            typeof(PermissionedScopedQuery).FullName!, "abc123");

        key.Should().Contain(PermissionVersion);
        key.Should().NotContain("default");
        key.Should().StartWith("notrelix:v1:test:permissioned:");
    }

    // ========================================================================
    // Phase 2: Cache Authorization Tests
    // ========================================================================

    [Fact]
    public async Task CacheHit_ReturnsCachedResponse_WithoutCallingHandler()
    {
        var cache = new Mock<IRedisCacheService>();
        cache.Setup(x => x.GetAsync<string>(It.IsAny<string>()))
            .ReturnsAsync("cached-value");

        var behavior = CreateBehavior<WorkspaceScopedQuery>(cache: cache);
        var handlerInvoked = false;

        var response = await behavior.Handle(
            new WorkspaceScopedQuery(),
            _ => { handlerInvoked = true; return Task.FromResult("fresh"); },
            default);

        response.Should().Be("cached-value");
        handlerInvoked.Should().BeFalse();
    }

    [Fact]
    public async Task CacheMiss_CallsHandler_AndStoresResult()
    {
        var cache = CreateCacheService();
        var behavior = CreateBehavior<WorkspaceScopedQuery>(cache: cache);

        var response = await behavior.Handle(
            new WorkspaceScopedQuery(),
            _ => Task.FromResult("fresh-value"),
            default);

        response.Should().Be("fresh-value");
        cache.Verify(x => x.SetAsync(It.IsAny<string>(), "fresh-value", It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task NonCacheableRequest_PassesThrough()
    {
        var cache = new Mock<IRedisCacheService>();
        var behavior = CreateBehavior<NonCacheableQuery>(cache: cache);
        var handlerInvoked = false;

        var response = await behavior.Handle(
            new NonCacheableQuery(),
            _ => { handlerInvoked = true; return Task.FromResult("passthrough"); },
            default);

        response.Should().Be("passthrough");
        handlerInvoked.Should().BeTrue();
        cache.Verify(x => x.GetAsync<string>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NullResponse_IsNotCached()
    {
        var cache = CreateCacheService();
        var behavior = CreateBehavior<WorkspaceScopedQuery>(cache: cache);

        var response = await behavior.Handle(
            new WorkspaceScopedQuery(),
            _ => Task.FromResult<string>(null!),
            default);

        response.Should().BeNull();
        cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task UnknownScope_ThrowsSecurityMisconfigurationException()
    {
        var behavior = CreateBehavior<UnknownScopeQuery>();

        var act = () => behavior.Handle(
            new UnknownScopeQuery(),
            _ => Task.FromResult("never"),
            default);

        await act.Should().ThrowAsync<SecurityMisconfigurationException>()
            .WithMessage("*Unknown AuthorizedCacheScope*");
    }

    [Fact]
    public async Task AccountScope_GeneratesCorrectKey()
    {
        var cache = CreateCacheService();
        var behavior = CreateBehavior<AccountScopedQuery>(cache: cache);

        await behavior.Handle(
            new AccountScopedQuery(),
            _ => Task.FromResult("response"),
            default);

        cache.Verify(x => x.GetAsync<string>(It.Is<string>(k => k.Contains("account"))), Times.Once);
        cache.Verify(x => x.GetAsync<string>(It.Is<string>(k => k.Contains(AccountId.ToString()))), Times.Once);
    }

    [Fact]
    public async Task WorkspaceScope_GeneratesCorrectKey()
    {
        var cache = CreateCacheService();
        var behavior = CreateBehavior<WorkspaceScopedQuery>(cache: cache);

        await behavior.Handle(
            new WorkspaceScopedQuery(),
            _ => Task.FromResult("response"),
            default);

        cache.Verify(x => x.GetAsync<string>(It.Is<string>(k => k.Contains("workspace"))), Times.Once);
        cache.Verify(x => x.GetAsync<string>(It.Is<string>(k => k.Contains(AccountId.ToString()))), Times.Once);
        cache.Verify(x => x.GetAsync<string>(It.Is<string>(k => k.Contains(WorkspaceId.ToString()))), Times.Once);
    }

    [Fact]
    public async Task UserScope_GeneratesCorrectKey()
    {
        var cache = CreateCacheService();
        var behavior = CreateBehavior<UserScopedQuery>(cache: cache);

        await behavior.Handle(
            new UserScopedQuery(),
            _ => Task.FromResult("response"),
            default);

        cache.Verify(x => x.GetAsync<string>(It.Is<string>(k => k.Contains("user"))), Times.Once);
        cache.Verify(x => x.GetAsync<string>(It.Is<string>(k => k.Contains(UserId.ToString()))), Times.Once);
    }

    [Fact]
    public async Task DifferentUsers_GetDifferentCacheKeys()
    {
        var factory = CreateKeyFactory();
        var requestName = typeof(WorkspaceScopedQuery).FullName!;
        var hash = "hash123";

        var keyUserA = factory.User(AccountId, WorkspaceId, Guid.NewGuid(), requestName, hash);
        var keyUserB = factory.User(AccountId, WorkspaceId, Guid.NewGuid(), requestName, hash);

        keyUserA.Should().NotBe(keyUserB);
    }

    [Fact]
    public async Task DifferentPermissionVersions_GetDifferentCacheKeys()
    {
        var factory = CreateKeyFactory();
        var requestName = typeof(PermissionedScopedQuery).FullName!;
        var hash = "hash123";

        var keyV1 = factory.Permissioned(AccountId, WorkspaceId, UserId, "v1", requestName, hash);
        var keyV2 = factory.Permissioned(AccountId, WorkspaceId, UserId, "v2", requestName, hash);

        keyV1.Should().NotBe(keyV2);
    }
}
