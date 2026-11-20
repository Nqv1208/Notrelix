using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.API;
using Notrelix.API.Realtime;
using Notrelix.Application.Features.Identity.Public.Access;
using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Auth.Jwt;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using StackExchange.Redis;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// A disabled person loses every session, refresh token, API token, issued access token and realtime connection
/// at once, over the real stack on the application login (the disabling caller runs as the worker, which is the
/// only login that may write another person's rows). The person's very next request is refused, and a person
/// who is not the target (on the application login) cannot disable anyone.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class UserDisableAppRoleTests : IAsyncLifetime
{
    private const string Password = "Correct-Horse-9!";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private User _bob = null!;
    private string _apiTokenSecret = string.Empty;
    private WebApplicationFactory<Program>? _factory;
    private ConnectionMultiplexer? _redis;

    public UserDisableAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        string hash;
        using (var scope = Factory.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);

        _a = await AppRoleTenants.SeedAsync(_fixture, "alice-disable@example.com", "Alice", hash);
        _bob = await AppRoleUsers.AddAsync(_fixture, "bob-disable@example.com", "Bob", hash);

        var secret = new ApiTokenSecretService().Generate();
        _apiTokenSecret = secret.RawToken;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ApiTokens.Add(ApiToken.Create(
            _a.AccountId, _a.WorkspaceId, _a.User.Id, "Alice token", secret.TokenHash,
            scopes: null, createdBy: _a.User.Id, createdAt: DateTimeOffset.UtcNow, expiresAt: null));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        _redis?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private IConnectionMultiplexer Redis => _redis ??= ConnectionMultiplexer.Connect(_fixture.RedisConnectionString);

    private static string TokensUrl(Guid workspaceId) => $"/api/v1/workspaces/{workspaceId}/api-tokens";

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        return client;
    }

    private HttpClient ApiTokenClient()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", _apiTokenSecret);
        return client;
    }

    private static readonly RlsOptions SessionContextOn = new() { SetSessionContext = true };

    /// <summary>Runs <see cref="IdentityUserAccessProvider"/> in one transaction on <paramref name="context"/>.</summary>
    private async Task<bool> EndAccessAsync(
        ApplicationDbContext context, FakeCurrentTenantContext tenant, Guid target, UserAccessEnd kind,
        Func<RlsSessionContext, Task> enterScope)
    {
        await using var tx = await context.Database.BeginTransactionAsync();
        var rls = new RlsSessionContext(
            context, Options.Create(SessionContextOn), tenant, NullLogger<RlsSessionContext>.Instance);
        await enterScope(rls);

        var provider = new IdentityUserAccessProvider(
            context, new JwtBlacklistService(Redis), FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow),
            activity: null!, notice: null!); // EndAccess records no activity and sends no notice
        var found = await provider.EndAccessAsync(target, Guid.CreateVersion7(), kind, "left the company", CancellationToken.None);
        await context.SaveChangesAsync();
        await tx.CommitAsync();
        return found;
    }

    private Task<bool> DisableAsWorkerAsync(Guid target, UserAccessEnd kind = UserAccessEnd.Deactivated)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        return EndAccessAsync(_fixture.CreateWorkerContext(system), system, target, kind, rls => rls.ApplyAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(UserAccessEnd.Deactivated)]
    [InlineData(UserAccessEnd.Suspended)]
    public async Task ADisabledPerson_LosesEverySessionRefreshAndApiToken_AtOnce_AndTheNextRequestIsRefused(UserAccessEnd kind)
    {
        using var device1 = await SignInAsync("alice-disable@example.com");
        using var device2 = await SignInAsync("alice-disable@example.com");
        using var apiToken = ApiTokenClient();
        using var bobClient = await SignInAsync("bob-disable@example.com");
        (await device1.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await device2.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await apiToken.GetAsync(TokensUrl(_a.WorkspaceId))).StatusCode.Should().Be(HttpStatusCode.OK);

        (await DisableAsWorkerAsync(_a.User.Id, kind)).Should().BeTrue();

        // The next request on each credential is refused.
        (await device1.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await device2.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await apiToken.GetAsync(TokensUrl(_a.WorkspaceId))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // No refresh succeeds and no new sign-in succeeds, with the same failure as a wrong password.
        (await device1.PostAsync("/api/v1/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await device2.PostAsync("/api/v1/auth/refresh", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var fresh = Factory.CreateClient();
        var login = await fresh.PostAsJsonAsync("/api/v1/auth/login", new { email = "alice-disable@example.com", password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Only the disabled person is affected.
        (await bobClient.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var check = _fixture.CreateOwnerContext(system);
        (await check.Users.AsNoTracking().SingleAsync(u => u.Id == _a.User.Id)).Status
            .Should().Be(kind == UserAccessEnd.Suspended ? UserStatus.Suspended : UserStatus.Inactive);
        (await check.Sessions.AsNoTracking().Where(s => s.UserId == _a.User.Id).ToListAsync())
            .Should().OnlyContain(s => s.Status == SessionStatus.Revoked);
        (await check.Sessions.AsNoTracking().Where(s => s.UserId == _bob.Id).ToListAsync())
            .Should().OnlyContain(s => s.Status == SessionStatus.Active);
        (await check.ApiTokens.AsNoTracking().SingleAsync(t => t.UserId == _a.User.Id)).RevokedReason
            .Should().Be(ApiTokenRevokedReason.UserDisabled);
    }

    [Fact]
    public async Task ARealtimeConnectionOfADisabledPerson_FailsItsNextCredentialCheck()
    {
        using var device = await SignInAsync("alice-disable@example.com");
        string jwt;
        using (var scope = Factory.Services.CreateScope())
            jwt = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(_a.User);
        var issuedAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        var principal = new RealtimePrincipal(
            _a.User.Id, "alice-disable@example.com", "Alice", Guid.NewGuid().ToString("N"), null,
            issuedAt, DateTimeOffset.UtcNow.AddMinutes(10), Guid.NewGuid());
        var validator = new JwtRealtimeCredentialValidator(new JwtBlacklistService(Redis), TimeProvider.System);
        (await validator.IsStillValidAsync(principal, CancellationToken.None)).Should().BeTrue();
        jwt.Should().NotBeNullOrEmpty();

        await DisableAsWorkerAsync(_a.User.Id);

        (await validator.IsStillValidAsync(principal, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task APersonOnTheApplicationLogin_CannotDisableAnotherPerson_RowLevelSecurityHidesTheTarget()
    {
        using var device = await SignInAsync("alice-disable@example.com");
        var tenant = new FakeCurrentTenantContext();
        tenant.SetUser(_bob.Id);

        var found = await EndAccessAsync(
            _fixture.CreateAppContext(tenant), tenant, _a.User.Id, UserAccessEnd.Deactivated,
            rls => rls.ApplyUserScopeAsync(_bob.Id, CancellationToken.None));

        found.Should().BeFalse("Bob's scope sees no row of Alice's, so nothing is changed");
        (await device.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ApiTokenClient().GetAsync(TokensUrl(_a.WorkspaceId))).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
