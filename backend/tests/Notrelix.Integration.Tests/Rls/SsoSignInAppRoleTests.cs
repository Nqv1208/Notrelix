using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API;
using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Oidc;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The company sign-in end to end over the production host on the application-role login, against a fake identity
/// provider that signs real ID tokens (served behind the guarded named HTTP client): the Owner sets up the provider and
/// the domain, the sign-in page routes the email, the browser is sent to the provider and comes back to the one callback,
/// and session cookies are issued. Also the enforcement refusals, the replay, MFA afterwards, the client secret staying
/// secret, and positive and negative controls on the new identity table and scope. Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class SsoSignInAppRoleTests : IAsyncLifetime
{
    private const string Issuer = "https://login.acme.example";
    private static readonly string PlainSecret = string.Concat((char)65, (char)97, (char)49, (char)33, Guid.NewGuid().ToString("N"));
    private static readonly string WrongPassword = string.Concat((char)65, (char)97, (char)49, (char)33, Guid.NewGuid().ToString("N"));
    private static readonly string Password = string.Concat((char)65, (char)97, (char)49, (char)33, Guid.NewGuid().ToString("N"));
    private const string Admin = "/api/v1/accounts/security-policy/sso";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private readonly FakeDomainTxtResolver _dns = new();
    private readonly FakeOidcDiscoveryClient _discovery = new();
    private readonly FakeOidcProvider _provider = new(Issuer, "client-1", PlainSecret);
    private readonly StringBuilder _everythingTheBrowserSaw = new();
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _acmeOwner = null!;
    private User _member = null!;
    private WebApplicationFactory<Program>? _factory;

    public SsoSignInAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString)
        .WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IDomainTxtResolver>();
            s.AddSingleton<IDomainTxtResolver>(_dns);
            s.RemoveAll<IOidcDiscoveryClient>();
            s.AddSingleton<IOidcDiscoveryClient>(_discovery);
            // The provider is reached through the guarded named client; here its connection is the fake provider.
            s.AddHttpClient(OidcGuardedHttp.ClientName).ConfigurePrimaryHttpMessageHandler(() => _provider);
        }));

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _discovery.Serve(Issuer);
        _provider.Email = "ann@acme.com";
        string hash;
        using (var f = AppRoleHost.CreateFactory(_fixture, _fixture.OwnerConnectionString))
        using (var scope = f.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);

        _a = await AppRoleTenants.SeedAsync(_fixture, "a-ssi@example.com", "Assi", hash);
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-ssi@example.com", "Bssi", hash);
        _acmeOwner = await AppRoleUsers.AddAsync(_fixture, "owner@acme.com", "AcmeOwner", hash);
        _member = await AppRoleUsers.AddAsync(_fixture, "member@acme.com", "AcmeMember", hash);
        await AddAccountMemberAsync(_a.AccountId, _a.User, AccountRole.Owner);
        await AddAccountMemberAsync(_b.AccountId, _b.User, AccountRole.Owner);
        await AddAccountMemberAsync(_a.AccountId, _acmeOwner, AccountRole.Owner);
        await AddAccountMemberAsync(_a.AccountId, _member, AccountRole.Member);
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private async Task AddAccountMemberAsync(Guid accountId, User user, AccountRole role)
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.AccountMembers.Add(AccountMember.Create(accountId, user.Id, role, user.Id, Now));
        ctx.AccessGrants.Add(new AccessGrant(
            accountId: accountId, workspaceId: null, userId: user.Id, sourceContext: "Account",
            membershipStatus: "Active", roleCodes: [role.ToString()], permissionCodes: [],
            isAccountAdmin: role is AccountRole.Owner or AccountRole.Admin, isWorkspaceAdmin: false, grantedAt: Now));
        await ctx.SaveChangesAsync();
    }

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
        return Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
    }

    private async Task OwnerExecAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<HttpResponseMessage> OwnerCallAsync(HttpMethod method, string path, object? body = null)
    {
        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(_a.User);
        using var client = Factory.CreateClient();
        using var request = new HttpRequestMessage(method, Admin + path);
        request.Headers.Authorization = new("Bearer", token);
        request.Headers.Add("X-Account-Id", _a.AccountId.ToString("D"));
        if (body is not null) request.Content = JsonContent.Create(body);
        return await Record(await client.SendAsync(request));
    }

    private async Task<HttpResponseMessage> Record(HttpResponseMessage response)
    {
        await response.Content.LoadIntoBufferAsync();
        _everythingTheBrowserSaw.AppendLine(response.ToString()).AppendLine(await response.Content.ReadAsStringAsync());
        return response;
    }

    /// <summary>One browser: it keeps the cookies the server sets and sends them back by path, like a real one.</summary>
    private HttpClient NewBrowser() => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private async Task<HttpResponseMessage> AnonymousAsync(HttpMethod method, string url, object? body = null, string? cookies = null, HttpClient? browser = null)
    {
        using var own = browser is null ? Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false }) : null;
        var client = browser ?? own!;
        using var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (cookies is not null) request.Headers.Add("Cookie", cookies);
        return await Record(await client.SendAsync(request));
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private static async Task<string> ErrorCodeAsync(HttpResponseMessage response) =>
        (await JsonAsync(response)).GetProperty("errorCode").GetString()!;

    /// <summary>The Owner of Account A configures a provider, claims and verifies acme.com, and (optionally) requires SSO.</summary>
    private async Task ConfigureAsync(bool require = false, bool jit = true, bool verifyDomain = true)
    {
        (await OwnerCallAsync(HttpMethod.Put, "/provider",
            new { name = "Acme SSO", issuerUrl = Issuer, clientId = "client-1", clientSecret = PlainSecret, jitProvisioning = jit }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var added = await OwnerCallAsync(HttpMethod.Post, "/domains", new { domain = "acme.com" });
        added.StatusCode.Should().Be(HttpStatusCode.Created);
        var row = await JsonAsync(added);
        if (!verifyDomain) return;
        _dns.Publish("acme.com", row.GetProperty("txtValue").GetString()!);
        var verified = await OwnerCallAsync(HttpMethod.Post, $"/domains/{row.GetProperty("id").GetGuid()}/verify");
        (await JsonAsync(verified)).GetProperty("status").GetString().Should().Be("Verified");
        if (require) (await OwnerCallAsync(HttpMethod.Put, "/require", new { required = true })).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>The sign-in page asks how the email signs in, starts the company sign-in, the provider answers and the browser lands on the callback.</summary>
    private async Task<HttpResponseMessage> SignInThroughTheProviderAsync(string email, string? returnUrl = null)
    {
        using var browser = NewBrowser();
        await StartAsync(browser, email, returnUrl);
        return await CallbackAsync(_provider.Code, _provider.State!, browser);
    }

    /// <summary>Starts a company sign-in in <paramref name="browser"/> (which receives the binding cookie) and lets the provider answer.</summary>
    private async Task<HttpResponseMessage> StartAsync(HttpClient browser, string email, string? returnUrl = null)
    {
        var started = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/sso/start", new { email, returnUrl }, browser: browser);
        started.StatusCode.Should().Be(HttpStatusCode.OK, await started.Content.ReadAsStringAsync());
        var url = (await JsonAsync(started)).GetProperty("authorizationUrl").GetString()!;
        url.Should().StartWith(Issuer + "/authorize?");
        _provider.Email = email;
        _provider.Follow(url);
        return started;
    }

    private Task<HttpResponseMessage> CallbackAsync(string code, string state, HttpClient? browser = null) =>
        AnonymousAsync(HttpMethod.Get, $"/api/v1/auth/sso/callback?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}", browser: browser);

    private static string? BindingOf(HttpResponseMessage response) =>
        SetCookies(response).Where(c => c.StartsWith("ssoBinding=", StringComparison.Ordinal))
            .Select(c => c["ssoBinding=".Length..].Split(';')[0]).FirstOrDefault(v => v.Length > 0);

    private static string LocationOf(HttpResponseMessage response) => response.Headers.Location!.OriginalString;

    private static IEnumerable<string> SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];

    // ── the whole flow ─────────────────────────────────────────────────────

    [Fact]
    public async Task TheOwnerSetsUp_TheEmailRoutes_AndANewPersonSignsInThroughTheProvider_WithSessionCookiesAndAMemberOnly()
    {
        await ConfigureAsync();

        var identify = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/identify", new { email = "Ann@Acme.com" });
        identify.StatusCode.Should().Be(HttpStatusCode.OK);
        var routed = await JsonAsync(identify);
        routed.GetProperty("method").GetString().Should().Be("sso");
        routed.GetProperty("sso").GetProperty("orgName").GetString().Should().Be("Assi Co");
        routed.GetProperty("sso").GetProperty("provider").GetString().Should().Be("oidc");
        routed.GetProperty("sso").GetProperty("providerLabel").GetString().Should().Be("Acme SSO");
        routed.GetProperty("sso").GetProperty("enforced").GetBoolean().Should().BeFalse();

        var callback = await SignInThroughTheProviderAsync("ann@acme.com", "/boards/1");

        callback.StatusCode.Should().Be(HttpStatusCode.Redirect);
        LocationOf(callback).Should().Be("/auth/callback?returnTo=%2Fboards%2F1");
        var cookies = SetCookies(callback).ToList();
        cookies.Should().Contain(c => c.StartsWith("accessToken=", StringComparison.Ordinal));
        cookies.Should().Contain(c => c.StartsWith("refreshToken=", StringComparison.Ordinal));

        var annId = Guid.Parse(await OwnerScalarAsync("SELECT id FROM identity.users WHERE normalized_email = 'ann@acme.com'"));
        (await OwnerScalarAsync("SELECT has_password_credential::text || '/' || email_confirmed::text FROM identity.users WHERE id = @u", ("u", annId))).Should().Be("false/true");
        (await OwnerScalarAsync("SELECT role || '/' || status FROM account.account_members WHERE account_id = @a AND user_id = @u", ("a", _a.AccountId), ("u", annId))).Should().Be("Member/Active");
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_members WHERE user_id = @u", ("u", annId))).Should().Be("1");
        (await OwnerScalarAsync("SELECT count(*) FROM workspace.workspace_members WHERE user_id = @u", ("u", annId))).Should().Be("0");
        (await OwnerScalarAsync("SELECT count(*) FROM account.accounts")).Should().Be("2", "no personal Account was created");
        (await OwnerScalarAsync("SELECT account_id::text || '/' || subject || '/' || issuer FROM identity.sso_identities WHERE user_id = @u", ("u", annId)))
            .Should().Be($"{_a.AccountId}/subject-1/{Issuer}");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u", ("u", annId))).Should().Be("1");

        // The cookie really is a session: the person's own profile answers.
        var accessCookie = cookies.First(c => c.StartsWith("accessToken=", StringComparison.Ordinal)).Split(';')[0];
        var me = await AnonymousAsync(HttpMethod.Get, "/api/v1/auth/me", cookies: accessCookie);
        me.StatusCode.Should().Be(HttpStatusCode.OK, await me.Content.ReadAsStringAsync());
        (await JsonAsync(me)).GetProperty("email").GetString().Should().Be("ann@acme.com");

        // The provider saw exactly one token exchange with the secret as Basic credentials, nowhere else.
        _provider.Requests.Where(r => r.Method == "POST").Should().ContainSingle();
        _everythingTheBrowserSaw.ToString().Should().NotContain(PlainSecret, "the client secret is in no response, header or redirect");
    }

    [Fact]
    public async Task AReplayedCallback_GivesNoSecondSession_AndAnUnknownStateIsRefused()
    {
        await ConfigureAsync();
        var first = await SignInThroughTheProviderAsync("ann@acme.com");
        first.StatusCode.Should().Be(HttpStatusCode.Redirect);

        var replay = await CallbackAsync(_provider.Code, _provider.State!);
        var mismatch = await CallbackAsync(_provider.Code, "some-other-state");

        LocationOf(replay).Should().Be("/sign-in?error=identity.sso.state-invalid");
        LocationOf(mismatch).Should().Be("/sign-in?error=identity.sso.state-invalid");
        SetCookies(replay).Concat(SetCookies(mismatch)).Should().OnlyContain(c => c.StartsWith("ssoBinding=;", StringComparison.Ordinal), "only the binding cookie is cleared");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions")).Should().Be("1", "only the first callback made a session");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.users WHERE normalized_email = 'ann@acme.com'")).Should().Be("1");
    }

    // ── browser binding (login CSRF) ───────────────────────────────────────

    [Fact]
    public async Task TheStartResponse_SetsTheBindingCookie_WithTheRightFlags_AndNeitherTheBodyNorTheAddressCarryIt()
    {
        await ConfigureAsync();
        using var browser = NewBrowser();

        var started = await StartAsync(browser, "ann@acme.com");

        var cookie = SetCookies(started).Single(c => c.StartsWith("ssoBinding=", StringComparison.Ordinal));
        cookie.Should().ContainEquivalentOf("path=/api/v1/auth/sso/callback").And.ContainEquivalentOf("samesite=lax")
            .And.ContainEquivalentOf("httponly").And.ContainEquivalentOf("max-age=600");
        var raw = BindingOf(started)!;
        raw.Length.Should().BeGreaterThanOrEqualTo(43, "32 random bytes, base64url");
        (await started.Content.ReadAsStringAsync()).Should().NotContain(raw);
        _provider.State.Should().NotContain(raw);
    }

    [Fact]
    public async Task TheCallbackAlwaysClearsTheBindingCookie_OnSuccessAndOnFailure()
    {
        await ConfigureAsync();
        using var browser = NewBrowser();
        await StartAsync(browser, "ann@acme.com");
        var success = await CallbackAsync(_provider.Code, _provider.State!, browser);

        using var stranger = NewBrowser();
        var failure = await CallbackAsync(_provider.Code, "unknown-state", stranger);

        foreach (var response in new[] { success, failure })
        {
            var cleared = SetCookies(response).Single(c => c.StartsWith("ssoBinding=;", StringComparison.Ordinal));
            cleared.Should().ContainEquivalentOf("path=/api/v1/auth/sso/callback").And.ContainEquivalentOf("expires=Thu, 01 Jan 1970").And.ContainEquivalentOf("httponly");
        }
        success.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task AVictimsBrowserOpeningTheAttackersCallback_HasNoBindingCookie_IsRefused_AndTheStateIsUsedUp()
    {
        await ConfigureAsync();
        using var attacker = NewBrowser();
        await StartAsync(attacker, "ann@acme.com");
        var (code, state) = (_provider.Code, _provider.State!);

        using var victim = NewBrowser();
        var forged = await CallbackAsync(code, state, victim);
        var legitimate = await CallbackAsync(code, state, attacker);

        LocationOf(forged).Should().Be("/sign-in?error=identity.sso.state-invalid");
        SetCookies(forged).Should().NotContain(c => c.StartsWith("accessToken=", StringComparison.Ordinal));
        LocationOf(legitimate).Should().Be("/sign-in?error=identity.sso.state-invalid", "the forged attempt used the single-use state up");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions")).Should().Be("0");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.users WHERE normalized_email = 'ann@acme.com'")).Should().Be("0");
        _provider.Requests.Should().BeEmpty("no token exchange happened for the forged callback");
    }

    [Fact]
    public async Task AVictimsOwnBindingCookieFromAnotherStart_DoesNotOpenTheAttackersCallback()
    {
        await ConfigureAsync();
        using var attacker = NewBrowser();
        await StartAsync(attacker, "ann@acme.com");
        var (code, state) = (_provider.Code, _provider.State!);
        using var victim = NewBrowser();
        await StartAsync(victim, "ann@acme.com");

        var forged = await CallbackAsync(code, state, victim);

        LocationOf(forged).Should().Be("/sign-in?error=identity.sso.state-invalid");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions")).Should().Be("0");
    }

    [Fact]
    public async Task AnUnverifiedDomain_RoutesNothing_AndStartAnswersTheSameRefusalAsAnUnknownDomain()
    {
        await ConfigureAsync(verifyDomain: false);

        var identify = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/identify", new { email = "ann@acme.com" });
        var unknownIdentify = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/identify", new { email = "ann@nobody.example" });
        var start = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/sso/start", new { email = "ann@acme.com" });
        var unknownStart = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/sso/start", new { email = "ann@nobody.example" });

        (await JsonAsync(identify)).ToString().Should().Be((await JsonAsync(unknownIdentify)).ToString());
        (await JsonAsync(identify)).GetProperty("method").GetString().Should().Be("password");
        start.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknownStart.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorCodeAsync(start)).Should().Be("identity.sso.unavailable");
        static string Shape(string body) => System.Text.RegularExpressions.Regex.Replace(body, "\"(traceId|correlationId)\":\"[^\"]*\"", "");
        Shape(await start.Content.ReadAsStringAsync()).Should().Be(Shape(await unknownStart.Content.ReadAsStringAsync()));
        _provider.Requests.Should().BeEmpty();
    }

    // ── enforcement ────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenSsoIsRequired_AMembersPasswordIsRefused_TheOwnersBreakGlassWorks_AWrongPasswordStaysInvalid_AndSwitchingItOffChangesNothingElse()
    {
        await ConfigureAsync(require: true);

        var refused = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/login", new { email = "member@acme.com", password = Password });
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ErrorCodeAsync(refused)).Should().Be("identity.auth.sso-required");
        SetCookies(refused).Should().BeEmpty();

        var wrong = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/login", new { email = "member@acme.com", password = WrongPassword });
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorCodeAsync(wrong)).Should().Be("identity.auth.invalid-credentials");

        var owner = await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/login", new { email = "owner@acme.com", password = Password });
        owner.StatusCode.Should().Be(HttpStatusCode.OK, "the Account Owner keeps the password as the break-glass");
        SetCookies(owner).Should().Contain(c => c.StartsWith("accessToken=", StringComparison.Ordinal));

        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u", ("u", _member.Id))).Should().Be("0", "the refused sign-in made no session");

        // A person on another domain is untouched by it.
        (await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/login", new { email = "a-ssi@example.com", password = Password })).StatusCode.Should().Be(HttpStatusCode.OK);

        // With it off, the same member signs in with a password.
        (await OwnerCallAsync(HttpMethod.Put, "/require", new { required = false })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await AnonymousAsync(HttpMethod.Post, "/api/v1/auth/login", new { email = "member@acme.com", password = Password })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── MFA still applies ──────────────────────────────────────────────────

    [Fact]
    public async Task WhenTheAccountRequiresMfa_ANewPersonOnlyGetsTheEnrolmentStep_NoSessionCookies()
    {
        await ConfigureAsync();
        await OwnerExecAsync(
            "INSERT INTO account.account_settings (id, account_id, setting_key, setting_value, created_at) VALUES (gen_random_uuid(), @a, 'security.require-mfa', 'true', now())",
            ("a", _a.AccountId));

        var callback = await SignInThroughTheProviderAsync("ann@acme.com");

        callback.StatusCode.Should().Be(HttpStatusCode.Redirect);
        LocationOf(callback).Should().Be("/sign-in?step=mfa&enroll=1");
        SetCookies(callback).Should().NotContain(c => c.StartsWith("accessToken=", StringComparison.Ordinal));
        SetCookies(callback).Should().Contain(c => c.StartsWith("notrelix_mfa", StringComparison.OrdinalIgnoreCase) || c.Contains("mfa", StringComparison.OrdinalIgnoreCase));
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions")).Should().Be("0");
    }

    [Fact]
    public async Task APersonWithASecondFactor_IsSentToTheChallenge_NotGivenASession()
    {
        await ConfigureAsync();
        await OwnerExecAsync(
            "INSERT INTO identity.sso_identities (id, account_id, issuer, subject, user_id, created_at) VALUES (gen_random_uuid(), @a, @i, 'subject-1', @u, now())",
            ("a", _a.AccountId), ("i", Issuer), ("u", _member.Id));
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            var factor = Notrelix.Domain.Identity.Mfa.UserMfaMethod.Create(
                _member.Id, Notrelix.Domain.Identity.Mfa.MfaMethodType.AuthenticatorApp, Now, Notrelix.Domain.SharedKernel.SecretRef.Create("s"));
            factor.Verify(Now);
            ctx.UserMfaMethods.Add(factor);
            await ctx.SaveChangesAsync();
        }

        var callback = await SignInThroughTheProviderAsync("member@acme.com");

        LocationOf(callback).Should().Be("/sign-in?step=mfa");
        SetCookies(callback).Should().NotContain(c => c.StartsWith("accessToken=", StringComparison.Ordinal));
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions")).Should().Be("0");
    }

    // ── the new identity table and its scope, on the application role ──────

    private async Task<T> AsAppAsync<T>(Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> body)
    {
        await using var conn = new NpgsqlConnection(_fixture.AppConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        return await body(conn, tx);
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection c, NpgsqlTransaction t, string sql, params (string, object)[] p)
    {
        await using var cmd = new NpgsqlCommand(sql, c, t);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
        return await cmd.ExecuteScalarAsync();
    }

    private async Task<int> TryNonQueryAsync(NpgsqlConnection c, NpgsqlTransaction t, string sql, params (string, object)[] p)
    {
        await using var save = new NpgsqlCommand("SAVEPOINT attempt", c, t);
        await save.ExecuteNonQueryAsync();
        try
        {
            await using var cmd = new NpgsqlCommand(sql, c, t);
            foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
            return await cmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42501")
        {
            await using var back = new NpgsqlCommand("ROLLBACK TO SAVEPOINT attempt", c, t);
            await back.ExecuteNonQueryAsync();
            return -1;
        }
    }

    private async Task SeedIdentitiesAsync()
    {
        await OwnerExecAsync(
            "INSERT INTO identity.sso_identities (id, account_id, issuer, subject, user_id, created_at) VALUES (gen_random_uuid(), @a, @i, 'subject-a', @u, now())",
            ("a", _a.AccountId), ("i", Issuer), ("u", _member.Id));
        await OwnerExecAsync(
            "INSERT INTO identity.sso_identities (id, account_id, issuer, subject, user_id, created_at) VALUES (gen_random_uuid(), @a, @i, 'subject-b', @u, now())",
            ("a", _b.AccountId), ("i", Issuer), ("u", _acmeOwner.Id));
    }

    [Fact]
    public async Task WithoutAScope_TheApplicationRoleReadsNoIdentityRow_AndCannotWriteOne()
    {
        await SeedIdentitiesAsync();

        var (count, inserted) = await AsAppAsync(async (c, t) =>
        {
            var n = (long)(await ScalarAsync(c, t, "SELECT count(*) FROM identity.sso_identities"))!;
            var w = await TryNonQueryAsync(c, t,
                "INSERT INTO identity.sso_identities (id, account_id, issuer, subject, user_id, created_at) VALUES (gen_random_uuid(), @a, @i, 'forged', @u, now())",
                ("a", _a.AccountId), ("i", Issuer), ("u", _member.Id));
            return (n, w);
        });

        count.Should().Be(0);
        inserted.Should().Be(-1, "no scope may write an identity row");
    }

    [Fact]
    public async Task TheIdentityScope_BindsTheOneUserOfTheSubject_ReadsOnlyThatUsersRows_AndWritesOnlyForThatUser()
    {
        await SeedIdentitiesAsync();
        var other = await AppRoleUsers.AddAsync(_fixture, "other@acme.com", "Other", "hash");

        var result = await AsAppAsync(async (c, t) =>
        {
            var user = await ScalarAsync(c, t, "SELECT ops.enter_sso_identity_scope(@a, 'subject-a')", ("a", _a.AccountId));
            var visible = (long)(await ScalarAsync(c, t, "SELECT count(*) FROM identity.sso_identities"))!;
            var visibleOfAccountB = (long)(await ScalarAsync(c, t, "SELECT count(*) FROM identity.sso_identities WHERE account_id = @a", ("a", _b.AccountId)))!;
            var otherUsersRowWritten = await TryNonQueryAsync(c, t,
                "INSERT INTO identity.sso_identities (id, account_id, issuer, subject, user_id, created_at) VALUES (gen_random_uuid(), @a, @i, 'x', @u, now())",
                ("a", Guid.NewGuid()), ("i", Issuer), ("u", other.Id));
            var ownRowWritten = await TryNonQueryAsync(c, t,
                "INSERT INTO identity.sso_identities (id, account_id, issuer, subject, user_id, created_at) VALUES (gen_random_uuid(), @a, @i, 'y', @u, now())",
                ("a", Guid.NewGuid()), ("i", Issuer), ("u", _member.Id));
            var updated = await TryNonQueryAsync(c, t, "UPDATE identity.sso_identities SET subject = 'changed'");
            var deleted = await TryNonQueryAsync(c, t, "DELETE FROM identity.sso_identities");
            return (user, visible, visibleOfAccountB, otherUsersRowWritten, ownRowWritten, updated, deleted);
        });

        result.user.Should().Be(_member.Id);
        result.visible.Should().Be(1, "only the bound user's own row");
        result.visibleOfAccountB.Should().Be(0, "no cross-account read");
        result.otherUsersRowWritten.Should().Be(-1, "a row for another user is refused");
        result.ownRowWritten.Should().Be(1, "the callback scope may link the very user it was entered for");
        result.updated.Should().BeOneOf(-1, 0);
        result.deleted.Should().BeOneOf(-1, 0);
        (await OwnerScalarAsync("SELECT count(*) FROM identity.sso_identities WHERE subject = 'changed'")).Should().Be("0");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.sso_identities WHERE subject = 'subject-a'")).Should().Be("1");
    }

    [Fact]
    public async Task TheIdentityScope_IsNotEntered_ForAnUnknownSubjectOrAnotherAccountsSubject()
    {
        await SeedIdentitiesAsync();

        var (unknown, wrongAccount, visible) = await AsAppAsync(async (c, t) =>
        {
            var u = await ScalarAsync(c, t, "SELECT ops.enter_sso_identity_scope(@a, 'nobody')", ("a", _a.AccountId));
            var w = await ScalarAsync(c, t, "SELECT ops.enter_sso_identity_scope(@a, 'subject-a')", ("a", _b.AccountId));
            var v = (long)(await ScalarAsync(c, t, "SELECT count(*) FROM identity.sso_identities"))!;
            return (u, w, v);
        });

        unknown.Should().BeOfType<DBNull>();
        wrongAccount.Should().BeOfType<DBNull>("a subject is only known inside the Account that bound it");
        visible.Should().Be(0);
    }

    [Fact]
    public async Task ANonCallbackScope_CannotWriteAnIdentityRow_EvenForItsOwnUser()
    {
        var written = await AsAppAsync(async (c, t) =>
        {
            // The ordinary signed-in (user) scope of someone: their own id, but not a sign-in scope.
            await ScalarAsync(c, t, "SELECT set_config('app.request_scope', 'user', true)");
            await ScalarAsync(c, t, "SELECT set_config('app.scope_user_id', @u, true)", ("u", _member.Id.ToString()));
            await ScalarAsync(c, t, "SELECT set_config('app.current_user_id', @u, true)", ("u", _member.Id.ToString()));
            return await TryNonQueryAsync(c, t,
                "INSERT INTO identity.sso_identities (id, account_id, issuer, subject, user_id, created_at) VALUES (gen_random_uuid(), @a, @i, 'self-made', @u, now())",
                ("a", _a.AccountId), ("i", Issuer), ("u", _member.Id));
        });

        written.Should().Be(-1);
        (await OwnerScalarAsync("SELECT count(*) FROM identity.sso_identities")).Should().Be("0");
    }
}
