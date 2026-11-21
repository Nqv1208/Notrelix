using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.API;
using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Public.Sso;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Enterprise single sign-on, the Accounts side, over the production host on the application-role login: only an
/// Account Owner configures it (every admin endpoint refuses an Admin, a Member and another tenant's Owner), the client
/// secret never appears in any response and is ciphertext in the table, a domain another Account verified cannot be
/// claimed and the database's partial unique index backs it, and the anonymous sign-in paths read single sign-on only
/// through the bounded database functions of 035_sso_scope.sql (positive and negative controls on the application role).
/// Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AccountSsoAppRoleTests : IAsyncLifetime
{
    private const string Issuer = "https://login.acme.example";
    private const string PlainSecret = "plain-client-secret-AbC123-never-leaks";
    private const string Base = "/api/v1/accounts/security-policy/sso";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private readonly FakeDomainTxtResolver _dns = new();
    private readonly FakeOidcDiscoveryClient _discovery = new();
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _acmeOwner = null!;
    private User _accountAdmin = null!;
    private User _accountMember = null!;
    private User _person = null!;
    private User _outsider = null!;
    private WebApplicationFactory<Program>? _factory;

    public AccountSsoAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString)
        .WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IDomainTxtResolver>();
            s.AddSingleton<IDomainTxtResolver>(_dns);
            s.RemoveAll<IOidcDiscoveryClient>();
            s.AddSingleton<IOidcDiscoveryClient>(_discovery);
        }));

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _discovery.Serve(Issuer);
        string hash;
        using (var f = AppRoleHost.CreateFactory(_fixture, _fixture.OwnerConnectionString))
        using (var scope = f.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword("Pw-x-9");

        _a = await AppRoleTenants.SeedAsync(_fixture, "a-sso@example.com", "Asso", hash);
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-sso@example.com", "Bsso", hash);
        _acmeOwner = await AppRoleUsers.AddAsync(_fixture, "owner@acme.com", "AcmeOwner", hash);
        _accountAdmin = await AppRoleUsers.AddAsync(_fixture, "admin-sso@example.com", "Adm", hash);
        _accountMember = await AppRoleUsers.AddAsync(_fixture, "member-sso@example.com", "Mem", hash);
        _person = await AppRoleUsers.AddAsync(_fixture, "person@acme.com", "Per", hash);
        _outsider = await AppRoleUsers.AddAsync(_fixture, "someone@elsewhere.example", "Out", hash);

        await AddAccountMemberAsync(_a.AccountId, _a.User, AccountRole.Owner);
        await AddAccountMemberAsync(_b.AccountId, _b.User, AccountRole.Owner);
        await AddAccountMemberAsync(_a.AccountId, _acmeOwner, AccountRole.Owner);
        await AddAccountMemberAsync(_a.AccountId, _accountAdmin, AccountRole.Admin);
        await AddAccountMemberAsync(_a.AccountId, _accountMember, AccountRole.Member);
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

    private async Task<HttpResponseMessage> AsAsync(User who, Guid accountId, HttpMethod method, string url, object? body = null)
    {
        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = Factory.CreateClient();
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new("Bearer", token);
        request.Headers.Add("X-Account-Id", accountId.ToString("D"));
        if (body is not null) request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private Task<HttpResponseMessage> OwnerCallAsync(HttpMethod method, string path, object? body = null) =>
        AsAsync(_a.User, _a.AccountId, method, Base + path, body);

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private static async Task<string> ErrorCodeAsync(HttpResponseMessage response) =>
        (await JsonAsync(response)).GetProperty("errorCode").GetString()!;

    private object ProviderBody(string? secret = PlainSecret, bool jit = true) =>
        new { name = "Acme SSO", issuerUrl = Issuer, clientId = "client-1", clientSecret = secret, jitProvisioning = jit };

    /// <summary>The Owner of Account A configures a provider, claims and verifies acme.com, and (optionally) requires SSO.</summary>
    private async Task ConfigureAsync(bool require = false)
    {
        (await OwnerCallAsync(HttpMethod.Put, "/provider", ProviderBody())).StatusCode.Should().Be(HttpStatusCode.OK);
        var added = await OwnerCallAsync(HttpMethod.Post, "/domains", new { domain = "acme.com" });
        added.StatusCode.Should().Be(HttpStatusCode.Created);
        var row = await JsonAsync(added);
        _dns.Publish("acme.com", row.GetProperty("txtValue").GetString()!);
        var verified = await OwnerCallAsync(HttpMethod.Post, $"/domains/{row.GetProperty("id").GetGuid()}/verify");
        (await JsonAsync(verified)).GetProperty("status").GetString().Should().Be("Verified");
        if (require) (await OwnerCallAsync(HttpMethod.Put, "/require", new { required = true })).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── permission: the Owner configures, nobody else ─────────────────────

    [Fact]
    public async Task EveryAdminEndpoint_RefusesAnAdmin_AMember_AndAnotherTenantsOwner_AndStoresNothing()
    {
        var someDomain = Guid.NewGuid();
        var calls = new (HttpMethod Method, string Path, object? Body)[]
        {
            (HttpMethod.Get, "", null),
            (HttpMethod.Put, "/provider", ProviderBody()),
            (HttpMethod.Delete, "/provider", null),
            (HttpMethod.Post, "/domains", new { domain = "acme.com" }),
            (HttpMethod.Post, $"/domains/{someDomain}/verify", null),
            (HttpMethod.Delete, $"/domains/{someDomain}", null),
            (HttpMethod.Put, "/require", new { required = true }),
        };

        foreach (var (who, name) in new[] { (_accountAdmin, "admin"), (_accountMember, "member") })
            foreach (var call in calls)
                (await AsAsync(who, _a.AccountId, call.Method, Base + call.Path, call.Body)).StatusCode
                    .Should().Be(HttpStatusCode.Forbidden, $"{name} {call.Method} {call.Path}");

        foreach (var call in calls)
        {
            var status = (await AsAsync(_b.User, _a.AccountId, call.Method, Base + call.Path, call.Body)).StatusCode;
            ((int)status).Should().BeGreaterThanOrEqualTo(400, $"another tenant's Owner {call.Method} {call.Path}");
        }

        (await OwnerScalarAsync("SELECT (SELECT count(*) FROM account.account_domains) + (SELECT count(*) FROM account.account_identity_providers) + (SELECT count(*) FROM account.account_settings)"))
            .Should().Be("0", "a refused call stores nothing");
        _discovery.Calls.Should().Be(0, "a refused call fetches nothing");
    }

    // ── the Owner's setup and the secret ──────────────────────────────────

    [Fact]
    public async Task TheOwnerConfiguresSso_TheSecretIsCiphertextInTheTable_AndNeverInAnyResponse()
    {
        var put = await OwnerCallAsync(HttpMethod.Put, "/provider", ProviderBody());
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await JsonAsync(put);
        saved.GetProperty("status").GetString().Should().Be("Active");
        saved.GetProperty("validationError").ValueKind.Should().Be(JsonValueKind.Null);

        var stored = await OwnerScalarAsync("SELECT client_secret_protected FROM account.account_identity_providers WHERE account_id = @a", ("a", _a.AccountId));
        stored.Should().NotBeNullOrEmpty().And.NotContain(PlainSecret, "the table holds ciphertext, not the secret");

        var added = await OwnerCallAsync(HttpMethod.Post, "/domains", new { domain = "Acme.com" });
        added.StatusCode.Should().Be(HttpStatusCode.Created);
        var row = await JsonAsync(added);
        row.GetProperty("domain").GetString().Should().Be("acme.com");
        row.GetProperty("status").GetString().Should().Be("Pending");
        row.GetProperty("txtHost").GetString().Should().Be("acme.com");
        var txt = row.GetProperty("txtValue").GetString()!;
        txt.Should().StartWith("notrelix-verify=");

        // idempotent add
        var again = await JsonAsync(await OwnerCallAsync(HttpMethod.Post, "/domains", new { domain = "acme.com" }));
        again.GetProperty("id").GetGuid().Should().Be(row.GetProperty("id").GetGuid());
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_domains")).Should().Be("1");

        // DNS miss: Pending, never an error
        var missed = await OwnerCallAsync(HttpMethod.Post, $"/domains/{row.GetProperty("id").GetGuid()}/verify");
        missed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(missed)).GetProperty("status").GetString().Should().Be("Pending");

        _dns.Publish("acme.com", txt);
        var verified = await JsonAsync(await OwnerCallAsync(HttpMethod.Post, $"/domains/{row.GetProperty("id").GetGuid()}/verify"));
        verified.GetProperty("status").GetString().Should().Be("Verified");
        verified.GetProperty("txtValue").ValueKind.Should().Be(JsonValueKind.Null);
        (await OwnerScalarAsync("SELECT verification_token_protected IS NULL FROM account.account_domains")).Should().Be("True");

        (await OwnerCallAsync(HttpMethod.Put, "/require", new { required = true })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var read = await OwnerCallAsync(HttpMethod.Get, "");
        var body = await read.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("canManage").GetBoolean().Should().BeTrue();
        doc.GetProperty("requireSso").GetBoolean().Should().BeTrue();
        doc.GetProperty("redirectUri").GetString().Should().EndWith("/api/v1/auth/sso/callback");
        var provider = doc.GetProperty("provider");
        provider.GetProperty("name").GetString().Should().Be("Acme SSO");
        provider.GetProperty("issuerUrl").GetString().Should().Be(Issuer);
        provider.GetProperty("clientId").GetString().Should().Be("client-1");
        provider.GetProperty("hasClientSecret").GetBoolean().Should().BeTrue();
        provider.GetProperty("status").GetString().Should().Be("Active");
        provider.GetProperty("jitProvisioning").GetBoolean().Should().BeTrue();

        // The secret appears in no body (GET, PUT, the domain rows) and no column other than the protected one.
        foreach (var text in new[] { body, await put.Content.ReadAsStringAsync(), await added.Content.ReadAsStringAsync() })
            text.Should().NotContain(PlainSecret).And.NotContain(stored);
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_identity_providers p WHERE p::text LIKE '%' || @s || '%'", ("s", PlainSecret)))
            .Should().Be("0", "no column of the provider row holds the plaintext");
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_domains d WHERE d::text LIKE '%' || @s || '%'", ("s", PlainSecret)))
            .Should().Be("0");
    }

    [Fact]
    public async Task ARefusedProviderStaysDraft_WithTheStatedReason_AndTheSecretStillNeverShows()
    {
        _discovery.Refuse(Issuer, OidcDiscoveryFailure.IssuerMismatch);

        var put = await OwnerCallAsync(HttpMethod.Put, "/provider", ProviderBody());

        var saved = await JsonAsync(put);
        saved.GetProperty("status").GetString().Should().Be("Draft");
        saved.GetProperty("validationError").GetString().Should().Be("issuer-mismatch");
        (await put.Content.ReadAsStringAsync()).Should().NotContain(PlainSecret);
        var read = await OwnerCallAsync(HttpMethod.Get, "");
        (await JsonAsync(read)).GetProperty("provider").GetProperty("validationError").GetString().Should().Be("issuer-mismatch");
        (await read.Content.ReadAsStringAsync()).Should().NotContain(PlainSecret);
    }

    [Fact]
    public async Task TheProviderIsReplacedByASecondSave_KeepingTheSecretWhenNoneIsGiven_AndTheErrorsNeverEchoIt()
    {
        await OwnerCallAsync(HttpMethod.Put, "/provider", ProviderBody());
        var before = await OwnerScalarAsync("SELECT client_secret_protected FROM account.account_identity_providers");

        (await OwnerCallAsync(HttpMethod.Put, "/provider", ProviderBody(secret: null, jit: false))).StatusCode.Should().Be(HttpStatusCode.OK);

        (await OwnerScalarAsync("SELECT client_secret_protected FROM account.account_identity_providers")).Should().Be(before);
        (await OwnerScalarAsync("SELECT jit_provisioning_enabled FROM account.account_identity_providers")).Should().Be("False");
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_identity_providers")).Should().Be("1");

        var bad = await OwnerCallAsync(HttpMethod.Put, "/provider", new { name = "x", issuerUrl = "http://insecure.example", clientId = "c", clientSecret = PlainSecret });
        bad.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ErrorCodeAsync(bad)).Should().Be("accounts.sso.invalid-issuer");
        (await bad.Content.ReadAsStringAsync()).Should().NotContain(PlainSecret, "a problem body never echoes the secret");

        (await OwnerCallAsync(HttpMethod.Delete, "/provider")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_identity_providers")).Should().Be("0");
    }

    // ── domains: claims, public providers, the index ──────────────────────

    [Fact]
    public async Task ADomainAnotherAccountVerified_IsRefusedOnAddAndOnVerify_AndTheIndexRefusesASecondVerifiedRow()
    {
        await ConfigureAsync(); // Account A verifies acme.com

        // Account B cannot claim it.
        var add = await AsAsync(_b.User, _b.AccountId, HttpMethod.Post, Base + "/domains", new { domain = "acme.com" });
        add.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(add)).Should().Be("accounts.domain.claimed");

        // B claimed a different Pending domain; A verifying "later.example" first, then B's verify is refused.
        var bRow = await JsonAsync(await AsAsync(_b.User, _b.AccountId, HttpMethod.Post, Base + "/domains", new { domain = "later.example" }));
        var aRow = await JsonAsync(await OwnerCallAsync(HttpMethod.Post, "/domains", new { domain = "later.example" }));
        _dns.Publish("later.example", aRow.GetProperty("txtValue").GetString()!);
        _dns.Publish("later.example", bRow.GetProperty("txtValue").GetString()!);
        (await JsonAsync(await OwnerCallAsync(HttpMethod.Post, $"/domains/{aRow.GetProperty("id").GetGuid()}/verify"))).GetProperty("status").GetString().Should().Be("Verified");
        var verifyB = await AsAsync(_b.User, _b.AccountId, HttpMethod.Post, Base + $"/domains/{bRow.GetProperty("id").GetGuid()}/verify");
        verifyB.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(verifyB)).Should().Be("accounts.domain.claimed");

        // The claimed flag shows on B's read.
        var bRead = await JsonAsync(await AsAsync(_b.User, _b.AccountId, HttpMethod.Get, Base));
        bRead.GetProperty("domains")[0].GetProperty("claimed").GetBoolean().Should().BeTrue();

        // The database is the last line: a second Verified row for the same domain violates the partial unique index.
        var act = async () => await OwnerExecAsync(
            "UPDATE account.account_domains SET verification_status = 'Verified' WHERE id = @id", ("id", bRow.GetProperty("id").GetGuid()));
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23505");
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_domains WHERE domain = 'later.example' AND verification_status = 'Verified'")).Should().Be("1");

        // ...while two Pending claims of one domain by different Accounts coexist (no squatting).
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_domains WHERE domain = 'later.example'")).Should().Be("2");
    }

    [Theory]
    [InlineData("gmail.com", "accounts.domain.public-provider")]
    [InlineData("Outlook.com", "accounts.domain.public-provider")]
    [InlineData("not a domain", "accounts.domain.invalid")]
    [InlineData("https://acme.com", "accounts.domain.invalid")]
    public async Task APublicMailProvider_OrAMalformedDomain_IsRefused_With422(string domain, string code)
    {
        var response = await OwnerCallAsync(HttpMethod.Post, "/domains", new { domain });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await ErrorCodeAsync(response)).Should().Be(code);
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_domains")).Should().Be("0");
    }

    [Fact]
    public async Task VerifyWhenDnsIsDown_StaysPending_AndAnotherAccountsRowIsNotFound()
    {
        var row = await JsonAsync(await OwnerCallAsync(HttpMethod.Post, "/domains", new { domain = "acme.com" }));
        _dns.Publish("acme.com", row.GetProperty("txtValue").GetString()!);
        _dns.Unavailable = true;

        var verify = await OwnerCallAsync(HttpMethod.Post, $"/domains/{row.GetProperty("id").GetGuid()}/verify");

        verify.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(verify)).GetProperty("status").GetString().Should().Be("Pending");

        var foreign = await AsAsync(_b.User, _b.AccountId, HttpMethod.Post, Base + $"/domains/{row.GetProperty("id").GetGuid()}/verify");
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── require-sso guards ────────────────────────────────────────────────

    [Fact]
    public async Task RequireSso_NeedsAnActiveProviderAndAVerifiedDomain_AndBlocksTheirRemovalWhileOn()
    {
        var early = await OwnerCallAsync(HttpMethod.Put, "/require", new { required = true });
        early.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(early)).Should().Be("accounts.sso.not-ready");

        await ConfigureAsync(require: true);
        var domainId = Guid.Parse(await OwnerScalarAsync("SELECT id FROM account.account_domains"));

        var removeProvider = await OwnerCallAsync(HttpMethod.Delete, "/provider");
        removeProvider.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(removeProvider)).Should().Be("accounts.sso.required-in-use");
        var removeDomain = await OwnerCallAsync(HttpMethod.Delete, $"/domains/{domainId}");
        removeDomain.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(removeDomain)).Should().Be("accounts.sso.required-in-use");

        (await OwnerCallAsync(HttpMethod.Put, "/require", new { required = false })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await OwnerCallAsync(HttpMethod.Delete, $"/domains/{domainId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await OwnerCallAsync(HttpMethod.Delete, "/provider")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── the bounded scope: the facts on the application role ──────────────

    private async Task<T> WithFactsAsync<T>(Func<IAccountSsoFacts, Task<T>> body)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();
        var result = await body(scope.ServiceProvider.GetRequiredService<IAccountSsoFacts>());
        await tx.CommitAsync();
        return result;
    }

    [Fact]
    public async Task Route_IsServedOnlyForAVerifiedDomainOfAnOpenAccountWithAnActiveProvider_AndIsTheSameNullForEveryOtherReason()
    {
        // Nothing configured yet.
        (await WithFactsAsync(f => f.FindRouteAsync("person@acme.com", default))).Should().BeNull();

        // A verified domain, but the provider is Draft.
        _discovery.Refuse(Issuer, OidcDiscoveryFailure.Unreachable);
        await OwnerCallAsync(HttpMethod.Put, "/provider", ProviderBody());
        var row = await JsonAsync(await OwnerCallAsync(HttpMethod.Post, "/domains", new { domain = "acme.com" }));
        (await WithFactsAsync(f => f.FindRouteAsync("person@acme.com", default))).Should().BeNull("a pending domain and a Draft provider route nothing");
        _dns.Publish("acme.com", row.GetProperty("txtValue").GetString()!);
        await OwnerCallAsync(HttpMethod.Post, $"/domains/{row.GetProperty("id").GetGuid()}/verify");
        (await WithFactsAsync(f => f.FindRouteAsync("person@acme.com", default))).Should().BeNull("a verified domain with a Draft provider routes nothing");

        // The provider becomes Active: the domain routes.
        _discovery.Serve(Issuer);
        await OwnerCallAsync(HttpMethod.Put, "/provider", ProviderBody(secret: null));
        var route = await WithFactsAsync(f => f.FindRouteAsync("Person@ACME.com", default));
        route.Should().BeEquivalentTo(new AccountSsoRoute(_a.AccountId, "Asso Co", "Acme SSO", false));
        (await WithFactsAsync(f => f.FindRouteAsync("nobody-at-all@acme.com", default))).Should().BeEquivalentTo(route, "a function of the domain only");

        // Other reasons: another domain, a subdomain, a closed Account - the same null.
        (await WithFactsAsync(f => f.FindRouteAsync("x@other.example", default))).Should().BeNull();
        (await WithFactsAsync(f => f.FindRouteAsync("x@eng.acme.com", default))).Should().BeNull();
        await OwnerExecAsync("UPDATE account.accounts SET status = 'Closed' WHERE id = @a", ("a", _a.AccountId));
        (await WithFactsAsync(f => f.FindRouteAsync("person@acme.com", default))).Should().BeNull("a closed Account does not route");
    }

    [Fact]
    public async Task Enforcement_OnTheApplicationRole_PasswordExemptsTheOwnerOnly_SocialExemptsNobody_OffHasNoEffect()
    {
        await ConfigureAsync();

        (await WithFactsAsync(f => f.CheckEnforcementAsync("person@acme.com", _person.Id, AccountSsoSignInMethod.Password, default)))
            .Required.Should().BeFalse("off has no effect");

        (await OwnerCallAsync(HttpMethod.Put, "/require", new { required = true })).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await WithFactsAsync(f => f.CheckEnforcementAsync("person@acme.com", _person.Id, AccountSsoSignInMethod.Password, default)))
            .Should().BeEquivalentTo(new AccountSsoEnforcement(true, _a.AccountId));
        (await WithFactsAsync(f => f.CheckEnforcementAsync("person@acme.com", _person.Id, AccountSsoSignInMethod.Social, default)))
            .Required.Should().BeTrue();
        (await WithFactsAsync(f => f.CheckEnforcementAsync("owner@acme.com", _acmeOwner.Id, AccountSsoSignInMethod.Password, default)))
            .Required.Should().BeFalse("the Owner's password is the break-glass");
        (await WithFactsAsync(f => f.CheckEnforcementAsync("owner@acme.com", _acmeOwner.Id, AccountSsoSignInMethod.Social, default)))
            .Required.Should().BeTrue("a social sign-in exempts nobody");
        (await WithFactsAsync(f => f.CheckEnforcementAsync("person@acme.com", null, AccountSsoSignInMethod.Password, default)))
            .Required.Should().BeTrue();
        (await WithFactsAsync(f => f.CheckEnforcementAsync("someone@elsewhere.example", _outsider.Id, AccountSsoSignInMethod.Social, default)))
            .Required.Should().BeFalse("another domain is not enforced");
    }

    [Fact]
    public async Task TheCallbackScope_HandsTheDecryptedSecretToIdentity_AndOnlyThere()
    {
        await ConfigureAsync();

        var provider = await WithFactsAsync(f => f.GetCallbackProviderAsync(_a.AccountId, default));

        provider.Should().NotBeNull();
        provider!.ClientSecret.Should().Be(PlainSecret);
        provider.ClientId.Should().Be("client-1");
        provider.Issuer.Should().Be(Issuer);
        (await WithFactsAsync(f => f.GetProviderAsync(_a.AccountId, default)))
            .Should().BeEquivalentTo(new AccountSsoProvider("Acme SSO", Issuer, "client-1", true));
        (await WithFactsAsync(f => f.GetCallbackProviderAsync(_b.AccountId, default))).Should().BeNull("another Account has no provider");
        (await WithFactsAsync(f => f.GetCallbackProviderAsync(Guid.NewGuid(), default))).Should().BeNull();
    }

    [Fact]
    public async Task Join_AddsOnlyAnAccountMemberRowOfRoleMember_ForAVerifiedDomainOfThisAccount()
    {
        await ConfigureAsync();
        var accountsBefore = await OwnerScalarAsync("SELECT count(*) FROM account.accounts");

        (await WithFactsAsync(f => f.JoinAsMemberAsync(_a.AccountId, _person.Id, "person@acme.com", DateTimeOffset.UtcNow, default))).Should().BeTrue();

        (await OwnerScalarAsync("SELECT role || '/' || status FROM account.account_members WHERE account_id = @a AND user_id = @u",
            ("a", _a.AccountId), ("u", _person.Id))).Should().Be("Member/Active");
        (await OwnerScalarAsync("SELECT array_to_string(role_codes, ',') || '/' || is_account_admin::text FROM authz.access_grants WHERE account_id = @a AND user_id = @u AND workspace_id IS NULL",
            ("a", _a.AccountId), ("u", _person.Id))).Should().Be("Member/false");
        (await OwnerScalarAsync("SELECT count(*) FROM workspace.workspace_members WHERE user_id = @u", ("u", _person.Id))).Should().Be("0", "never a workspace role");
        (await OwnerScalarAsync("SELECT count(*) FROM account.accounts")).Should().Be(accountsBefore, "no personal Account is created");

        // idempotent
        (await WithFactsAsync(f => f.JoinAsMemberAsync(_a.AccountId, _person.Id, "person@acme.com", DateTimeOffset.UtcNow, default))).Should().BeTrue();
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_members WHERE user_id = @u", ("u", _person.Id))).Should().Be("1");

        // an ended membership is never reactivated
        await OwnerExecAsync("UPDATE account.account_members SET status = 'Suspended' WHERE user_id = @u", ("u", _person.Id));
        (await WithFactsAsync(f => f.JoinAsMemberAsync(_a.AccountId, _person.Id, "person@acme.com", DateTimeOffset.UtcNow, default))).Should().BeFalse();
        (await OwnerScalarAsync("SELECT status FROM account.account_members WHERE user_id = @u", ("u", _person.Id))).Should().Be("Suspended");
    }

    [Fact]
    public async Task Join_IsRefused_ForAPersonWhoseEmailIsNotOnAVerifiedDomain_ForAnotherAccount_AndForAnUnconfirmedEmail()
    {
        await ConfigureAsync();
        var unconfirmed = await AppRoleUsers.AddAsync(_fixture, "late@acme.com", "Late", "hash", confirmEmail: false);

        (await WithFactsAsync(f => f.JoinAsMemberAsync(_a.AccountId, _outsider.Id, "someone@elsewhere.example", DateTimeOffset.UtcNow, default))).Should().BeFalse();
        (await WithFactsAsync(f => f.JoinAsMemberAsync(_b.AccountId, _person.Id, "person@acme.com", DateTimeOffset.UtcNow, default))).Should().BeFalse();
        (await WithFactsAsync(f => f.JoinAsMemberAsync(_a.AccountId, unconfirmed.Id, "late@acme.com", DateTimeOffset.UtcNow, default))).Should().BeFalse();
        // a lying caller: the database re-checks the person's own address, not the one passed in
        (await WithFactsAsync(f => f.JoinAsMemberAsync(_a.AccountId, _outsider.Id, "person@acme.com", DateTimeOffset.UtcNow, default))).Should().BeFalse();

        (await OwnerScalarAsync("SELECT count(*) FROM account.account_members WHERE user_id = ANY(@u)",
            ("u", new[] { _outsider.Id, _person.Id, unconfirmed.Id }))).Should().Be("0");
    }

    // ── negative controls on the application role ─────────────────────────

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

    [Theory]
    [InlineData("account.account_domains")]
    [InlineData("account.account_identity_providers")]
    [InlineData("account.account_settings")]
    [InlineData("account.account_members")]
    [InlineData("account.accounts")]
    public async Task WithoutAnyScope_TheApplicationRoleReadsNoRowOfTheSsoTables(string table)
    {
        await ConfigureAsync(require: true);

        var count = await AsAppAsync(async (c, t) =>
        {
            try { return (long)(await ScalarAsync(c, t, $"SELECT count(*) FROM {table}"))!; }
            catch (PostgresException ex) when (ex.SqlState == "42501") { return 0L; }
        });

        count.Should().Be(0);
    }

    [Theory]
    [InlineData("account.account_domains")]
    [InlineData("account.account_identity_providers")]
    [InlineData("account.account_settings")]
    [InlineData("account.account_members")]
    [InlineData("account.accounts")]
    [InlineData("workspace.workspaces")]
    [InlineData("identity.users")]
    public async Task InsideTheCallbackScope_NoTableIsReadable_TheScopeOnlyUnlocksTheSecretFunction(string table)
    {
        await ConfigureAsync(require: true);

        var (entered, count, secret) = await AsAppAsync(async (c, t) =>
        {
            var ok = (bool)(await ScalarAsync(c, t, "SELECT ops.enter_sso_callback_scope(@a)", ("a", _a.AccountId)))!;
            long n;
            try { n = (long)(await ScalarAsync(c, t, $"SELECT count(*) FROM {table}"))!; }
            catch (PostgresException ex) when (ex.SqlState == "42501") { n = 0; }
            var s = await ScalarAsync(c, t, "SELECT ops.sso_callback_secret()");
            return (ok, n, s as string);
        });

        entered.Should().BeTrue();
        count.Should().Be(0, $"the callback scope reads no row of {table}");
        secret.Should().NotBeNullOrEmpty().And.NotContain(PlainSecret, "the scope hands out the protected value only");
    }

    [Fact]
    public async Task TheSecret_IsNotReadableOutsideTheCallbackScope_NorForAnotherAccount_NorForAnInactiveProvider()
    {
        await ConfigureAsync();
        // Account B has an Active provider too.
        (await AsAsync(_b.User, _b.AccountId, HttpMethod.Put, Base + "/provider",
            new { name = "B SSO", issuerUrl = Issuer, clientId = "client-b", clientSecret = "b-secret-value" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var aSecret = await OwnerScalarAsync("SELECT client_secret_protected FROM account.account_identity_providers WHERE account_id = @a", ("a", _a.AccountId));
        var bSecret = await OwnerScalarAsync("SELECT client_secret_protected FROM account.account_identity_providers WHERE account_id = @a", ("a", _b.AccountId));

        var outside = await AsAppAsync((c, t) => ScalarAsync(c, t, "SELECT ops.sso_callback_secret()"));
        outside.Should().BeOfType<DBNull>("no scope, no secret");

        var inScopeA = await AsAppAsync(async (c, t) =>
        {
            await ScalarAsync(c, t, "SELECT ops.enter_sso_callback_scope(@a)", ("a", _a.AccountId));
            return (string)(await ScalarAsync(c, t, "SELECT ops.sso_callback_secret()"))!;
        });
        inScopeA.Should().Be(aSecret).And.NotBe(bSecret, "the scope of one Account never reads another Account's secret");

        // The scope is not entered for an Account whose provider is not Active, or that does not exist.
        await OwnerExecAsync("UPDATE account.account_identity_providers SET status = 'Disabled' WHERE account_id = @a", ("a", _b.AccountId));
        var (enteredB, secretB, enteredNone) = await AsAppAsync(async (c, t) =>
        {
            var b = (bool)(await ScalarAsync(c, t, "SELECT ops.enter_sso_callback_scope(@a)", ("a", _b.AccountId)))!;
            var s = await ScalarAsync(c, t, "SELECT ops.sso_callback_secret()");
            var none = (bool)(await ScalarAsync(c, t, "SELECT ops.enter_sso_callback_scope(@a)", ("a", Guid.NewGuid())))!;
            return (b, s, none);
        });
        enteredB.Should().BeFalse();
        secretB.Should().BeOfType<DBNull>();
        enteredNone.Should().BeFalse();

        // Another scope kind never unlocks the secret: a (form-submit style) token scope with another purpose.
        var forged = await AsAppAsync(async (c, t) =>
        {
            await ScalarAsync(c, t, "SELECT set_config('app.request_scope', 'token', true)");
            await ScalarAsync(c, t, "SELECT set_config('app.scope_token_purpose', 'form-submit', true)");
            await ScalarAsync(c, t, "SELECT set_config('app.scope_account_id', @a, true)", ("a", _a.AccountId.ToString()));
            return await ScalarAsync(c, t, "SELECT ops.sso_callback_secret()");
        });
        forged.Should().BeOfType<DBNull>("only the callback scope's purpose reads the secret");
    }

    [Fact]
    public async Task TheApplicationRole_CannotWriteTheSsoTables_Directly_EvenInsideTheCallbackScope()
    {
        await ConfigureAsync(require: true);

        var rowsChanged = await AsAppAsync(async (c, t) =>
        {
            await ScalarAsync(c, t, "SELECT ops.enter_sso_callback_scope(@a)", ("a", _a.AccountId));
            var changed = 0;
            foreach (var sql in new[]
            {
                "UPDATE account.account_identity_providers SET status = 'Disabled'",
                "UPDATE account.account_domains SET verification_status = 'Rejected'",
                "UPDATE account.account_settings SET setting_value = 'false'",
                "DELETE FROM account.account_identity_providers",
                "DELETE FROM account.account_domains",
            })
            {
                await using var cmd = new NpgsqlCommand(sql, c, t);
                try { changed += await cmd.ExecuteNonQueryAsync(); }
                catch (PostgresException ex) when (ex.SqlState == "42501") { /* not even granted */ }
            }

            return changed;
        });
        rowsChanged.Should().Be(0);

        var insert = async () => await AsAppAsync(async (c, t) =>
        {
            await ScalarAsync(c, t, "SELECT ops.enter_sso_callback_scope(@a)", ("a", _a.AccountId));
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO account.account_members (id, account_id, user_id, role, status, created_at) VALUES (gen_random_uuid(), @a, @u, 'Member', 'Active', now())", c, t);
            cmd.Parameters.AddWithValue("a", _a.AccountId);
            cmd.Parameters.AddWithValue("u", _person.Id);
            return await cmd.ExecuteNonQueryAsync();
        });
        (await insert.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("42501", "the member join is the function, not a policy on the table");

        (await OwnerScalarAsync("SELECT status FROM account.account_identity_providers")).Should().Be("Active");
        (await OwnerScalarAsync("SELECT verification_status FROM account.account_domains")).Should().Be("Verified");
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_members WHERE user_id = @u", ("u", _person.Id))).Should().Be("0");
    }

    [Fact]
    public async Task TheRouteFunction_AnswersNothingForPendingOrRejectedClaims_AndNeverCarriesTheProviderConfiguration()
    {
        await ConfigureAsync();
        await OwnerExecAsync("UPDATE account.account_domains SET verification_status = 'Pending'");

        var pending = await AsAppAsync(async (c, t) => (long)(await ScalarAsync(c, t, "SELECT count(*) FROM ops.sso_route('acme.com')"))!);
        pending.Should().Be(0);

        await OwnerExecAsync("UPDATE account.account_domains SET verification_status = 'Verified'");
        var columns = await AsAppAsync(async (c, t) =>
        {
            await using var cmd = new NpgsqlCommand("SELECT * FROM ops.sso_route('acme.com')", c, t);
            await using var r = await cmd.ExecuteReaderAsync();
            return Enumerable.Range(0, r.FieldCount).Select(r.GetName).ToArray();
        });
        columns.Should().BeEquivalentTo(["account_id", "account_name", "provider_name", "enforced"],
            "the route reveals no issuer, client id or secret");
    }
}
