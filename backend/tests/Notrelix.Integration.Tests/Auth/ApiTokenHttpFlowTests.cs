using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Notrelix.API;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// P12-BLK-001F — API-token principal boundary over the real HTTP production
/// graph (P12-BLK-001A scheme dispatch, 001B trusted claims, 001C credential
/// context, 001D TenantBootstrap enforcement, A3 workspace escape proof).
///
/// Bogus tokens must never fall through to the JWT handler; valid tokens must
/// authenticate; a token bound to workspace A must never operate workspace B
/// even when the same user has governance access to both.
/// </summary>
[Collection("Cache")]
public sealed class ApiTokenHttpFlowTests : IAsyncLifetime
{
    private static readonly Guid AccountId = Guid.Parse("A0000000-0000-0000-0000-0000000000A1");
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly CacheTestContainer _fixture;
    private User _user = null!;
    private Guid _workspaceA;
    private Guid _workspaceB;
    private string _rawSecret = string.Empty;

    public ApiTokenHttpFlowTests(CacheTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();

        _user = User.Create("apitoken-http@example.com", "HTTP Token User", "hashed", Now, hasPasswordCredential: true);

        var secretService = new ApiTokenSecretService();
        var secret = secretService.Generate();
        _rawSecret = secret.RawToken;

        await using var context = CreateSystemContext();
        var workspaceA = Workspace.Create(AccountId, _user.Id, "Workspace A", $"ws-a-{Guid.NewGuid():N}"[..16], Now);
        var workspaceB = Workspace.Create(AccountId, _user.Id, "Workspace B", $"ws-b-{Guid.NewGuid():N}"[..16], Now);
        _workspaceA = workspaceA.Id;
        _workspaceB = workspaceB.Id;

        context.Users.Add(_user);
        context.Workspaces.AddRange(workspaceA, workspaceB);
        context.WorkspaceMembers.AddRange(
            WorkspaceMember.Create(AccountId, _workspaceA, _user.Id, WorkspaceRole.Owner, _user.Id, Now),
            WorkspaceMember.Create(AccountId, _workspaceB, _user.Id, WorkspaceRole.Owner, _user.Id, Now));
        context.AccessGrants.Add(CreateGrant(_workspaceA));
        context.AccessGrants.Add(CreateGrant(_workspaceB));
        context.ApiTokens.Add(ApiToken.Create(
            AccountId, _workspaceA, _user.Id, "HTTP flow token", secret.TokenHash,
            scopes: null, createdBy: _user.Id, createdAt: Now, expiresAt: null));
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private AccessGrant CreateGrant(Guid workspaceId) => new(
        accountId: AccountId,
        workspaceId: workspaceId,
        userId: _user.Id,
        sourceContext: "Workspace",
        membershipStatus: "Active",
        roleCodes: [WorkspaceRole.Owner.ToString()],
        permissionCodes: [],
        isAccountAdmin: false,
        isWorkspaceAdmin: true,
        grantedAt: Now);

    private ApplicationDbContext CreateSystemContext()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return _fixture.CreatePostgresContext(tenant);
    }

    private static string ListTokensUrl(Guid workspaceId)
        => $"/api/v1/workspaces/{workspaceId}/api-tokens";

    private WebApplicationFactory<Program> CreateFactory(Action<Dictionary<string, string?>>? configure = null)
    {
        var keysPath = Path.Combine(
            Path.GetTempPath(), "notrelix-apitoken-http-test-keys", Guid.NewGuid().ToString("N"));

        var baseConfig = new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotrelixDb"] = _fixture.PostgresConnectionString,
            ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
            ["JwtSettings:SecretKey"] = "api-token-http-test-signing-key-ThisIsNotASecret-0123456789",
            ["JwtSettings:Issuer"] = "https://notrelix.test",
            ["JwtSettings:Audience"] = "notrelix-api",
            ["Cors:AllowedOrigins:0"] = "https://app.notrelix.test",
            ["Frontend:AppBaseUrl"] = "https://app.notrelix.test",
            ["Rls:Enabled"] = "true",
            ["Rls:SetSessionContext"] = "true",
            ["Smtp:Enabled"] = "true",
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "587",
            ["Smtp:FromEmail"] = "graph@notrelix.test",
            ["DataProtection:PersistKeys"] = "true",
            ["DataProtection:KeysPath"] = keysPath,
            ["Messaging:Transport"] = "InMemory",
            ["Billing:Mode"] = "Database",
            ["Storage:Mode"] = "Local",
            ["ForwardedHeaders:Enabled"] = "true",
            ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1"
        };

        var overrides = new Dictionary<string, string?>();
        configure?.Invoke(overrides);

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
                });

                builder.UseEnvironment("Production");

                builder.ConfigureTestServices(services =>
                {
                    // Membership resolution is mid-flight WIP owned by another
                    // worktree: IResourceAuthorizationSnapshotStore is not yet
                    // registered and the current PermissionService cannot see
                    // workspace members before TenantBootstrap sets the tenant
                    // context. P12 proves the committed auth-graph layer, so
                    // the permission evaluator is stubbed to owner-allow while
                    // every other production capability (scheme dispatch,
                    // token binding, TenantBootstrap enforcement, RLS session)
                    // runs real.
                    services.RemoveAll<Notrelix.Application.Common.Security.IPermissionEvaluator>();
                    services.AddScoped<Notrelix.Application.Common.Security.IPermissionEvaluator>(_ =>
                        Mock.Of<Notrelix.Application.Common.Security.IPermissionEvaluator>(e =>
                            e.EvaluateAsync(
                                It.IsAny<Notrelix.Application.Common.Security.PermissionContext>(),
                                It.IsAny<CancellationToken>())
                            == System.Threading.Tasks.Task.FromResult(
                                new Notrelix.Application.Common.Security.PermissionDecision(
                                    true, null, Notrelix.Domain.Governance.Permissions.PermissionLevel.Owner))));
                    services.RemoveAll<Notrelix.Application.Common.Security.IResourceAuthorizationSnapshotStore>();
                    services.AddScoped<Notrelix.Application.Common.Security.IResourceAuthorizationSnapshotStore>(_ =>
                        Mock.Of<Notrelix.Application.Common.Security.IResourceAuthorizationSnapshotStore>());
                });

                foreach (var (key, value) in baseConfig)
                {
                    if (!overrides.ContainsKey(key))
                    {
                        builder.UseSetting(key, value);
                    }
                }

                foreach (var (key, value) in overrides)
                {
                    if (value is null)
                    {
                        throw new InvalidOperationException(
                            $"Negative override for '{key}' must provide a value, not null.");
                    }

                    builder.UseSetting(key, value);
                }
            });
    }

    private string IssueJwtForUser()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IJwtService>()
            .GenerateAccessToken(_user);
    }

    [Fact]
    public async Task ValidApiToken_AuthenticatesThroughApiTokenScheme()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _rawSecret);

        var response = await client.GetAsync(ListTokensUrl(_workspaceA));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(
            System.Net.HttpStatusCode.OK,
            $"a valid API token must be dispatched to the ApiToken scheme, not rejected as an unparseable JWT. Body: {body}");
    }

    [Fact]
    public async Task InvalidNtkToken_IsRejected_With401()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "ntk_v1.never-issued-secret");

        var response = await client.GetAsync(ListTokensUrl(_workspaceA));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NtkV2Prefix_Token_RoutesToJwt_AndFailsClosed()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "ntk_v2.some-secret");

        var response = await client.GetAsync(ListTokensUrl(_workspaceA));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NonNtkBearerToken_RoutesToJwt_AndFailsClosed()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-an-api-token");

        var response = await client.GetAsync(ListTokensUrl(_workspaceA));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidJwtBearer_StillAuthenticates()
    {
        var jwt = IssueJwtForUser();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync(ListTokensUrl(_workspaceA));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            "JWT bearer must continue to authenticate after the composite policy scheme is introduced");
    }

    [Fact]
    public async Task AccessTokenCookie_StillAuthenticatesBrowserFlow()
    {
        var jwt = IssueJwtForUser();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"accessToken={jwt}");

        var response = await client.GetAsync(ListTokensUrl(_workspaceA));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            "the browser accessToken cookie flow must continue to route through the JWT handler");
    }

    [Fact]
    public async Task ApiTokenHeader_WinsOverCookiePrecedence()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _rawSecret);
        client.DefaultRequestHeaders.Add("Cookie", "accessToken=not-a-valid-jwt");

        var response = await client.GetAsync(ListTokensUrl(_workspaceA));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            "an explicit API token Authorization header must win over a stale browser cookie");
    }

    [Fact]
    public async Task TokenBoundToWorkspaceA_CannotOperateWorkspaceB()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _rawSecret);

        var response = await client.GetAsync(ListTokensUrl(_workspaceB));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden,
            $"a token bound to workspace A must never operate workspace B, even when the user can access both. Body: {body}");
    }

    [Fact]
    public async Task SameUser_JwtCanOperateWorkspaceB_ProvingTokenDenialIsCredentialScoped()
    {
        var jwt = IssueJwtForUser();
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync(ListTokensUrl(_workspaceB));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            "the same user via a session credential can operate workspace B; only the API token is workspace-bound");
    }
}