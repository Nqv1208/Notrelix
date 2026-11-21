using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The Account MFA requirement over the real production graph on the application role: only an Account Owner
/// turns it on; a person of such an Account with no factor gets no tokens at sign-in, only a token that opens
/// the enrollment step (and nothing else); finishing enrollment finishes the sign-in; refresh is refused for
/// them; a person who belongs to no requiring Account is untouched, and one requiring Account is enough when
/// the person belongs to several.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AccountMfaPolicyAppRoleTests : IAsyncLifetime
{
    private static readonly string Password = $"Pw-{Guid.NewGuid():N}-9";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _accountAdmin = null!;
    private User _accountMember = null!;
    private User _inBoth = null!;
    private WebApplicationFactory<Program>? _factory;

    public AccountMfaPolicyAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory =>
        _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        string hash;
        using (var f = AppRoleHost.CreateFactory(_fixture, _fixture.OwnerConnectionString))
        using (var scope = f.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);

        _a = await AppRoleTenants.SeedAsync(_fixture, "a-mfapolicy@example.com", "Aaa", hash);
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-mfapolicy@example.com", "Bbb", hash);
        _accountAdmin = await AppRoleUsers.AddAsync(_fixture, "admin-mfapolicy@example.com", "Adm", hash);
        _accountMember = await AppRoleUsers.AddAsync(_fixture, "member-mfapolicy@example.com", "Mem", hash);
        _inBoth = await AppRoleUsers.AddAsync(_fixture, "both-mfapolicy@example.com", "Both", hash);

        await AddAccountMemberAsync(_a.AccountId, _a.User, AccountRole.Owner);
        await AddAccountMemberAsync(_b.AccountId, _b.User, AccountRole.Owner);
        await AddAccountMemberAsync(_a.AccountId, _accountAdmin, AccountRole.Admin);
        await AddAccountMemberAsync(_a.AccountId, _accountMember, AccountRole.Member);
        await AddAccountMemberAsync(_a.AccountId, _inBoth, AccountRole.Member);
        await AddAccountMemberAsync(_b.AccountId, _inBoth, AccountRole.Member);
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

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

    private async Task<HttpResponseMessage> AsAccountMemberAsync(
        User who, Guid accountId, HttpMethod method, string url, object? body = null)
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

    private Task<HttpResponseMessage> SetPolicyAsync(User who, Guid accountId, bool required) =>
        AsAccountMemberAsync(who, accountId, HttpMethod.Put, "/api/v1/accounts/security-policy/mfa", new { required });

    private async Task<HttpResponseMessage> PostAsync(string url, object body, string? cookie = null)
    {
        using var client = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(request);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private Task<HttpResponseMessage> LoginAsync(string email) =>
        PostAsync("/api/v1/auth/login", new { email, password = Password });

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private static bool SetsSessionCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
        && cookies.Any(c => c.StartsWith("accessToken=", StringComparison.OrdinalIgnoreCase)
            || c.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));

    private static string TotpCode(string base32Secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = base32Secret.TrimEnd('=').ToUpperInvariant()
            .SelectMany(c => Convert.ToString(alphabet.IndexOf(c), 2).PadLeft(5, '0')).ToArray();
        var key = Enumerable.Range(0, bits.Length / 8)
            .Select(i => Convert.ToByte(new string(bits, i * 8, 8), 2)).ToArray();
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    /// <summary>Signs the person in under a policy that requires MFA and enrols a factor: returns its secret.</summary>
    private async Task<string> EnrolAsync(string email)
    {
        var login = await JsonAsync(await LoginAsync(email));
        var token = login.GetProperty("mfaEnrollmentToken").GetString()!;
        var start = await PostAsync("/api/v1/auth/mfa/required-enrollment/start", new { enrollmentToken = token });
        start.StatusCode.Should().Be(HttpStatusCode.OK, await start.Content.ReadAsStringAsync());
        var started = await JsonAsync(start);
        var secret = started.GetProperty("secret").GetString()!;
        var done = await PostAsync("/api/v1/auth/mfa/required-enrollment/complete", new
        {
            enrollmentToken = token,
            mfaMethodId = started.GetProperty("mfaMethodId").GetGuid(),
            code = TotpCode(secret)
        });
        done.StatusCode.Should().Be(HttpStatusCode.OK, await done.Content.ReadAsStringAsync());
        return secret;
    }

    [Fact]
    public async Task OnlyAnAccountOwnerCanChangeThePolicy_AnAdminAndAMemberCannot_AndEveryMemberCanReadIt()
    {
        (await SetPolicyAsync(_accountAdmin, _a.AccountId, true)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SetPolicyAsync(_accountMember, _a.AccountId, true)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_settings WHERE setting_key = 'security.require-mfa'"))
            .Should().Be("0", "a refused write stores nothing");

        // The owner of another Account cannot reach this Account's policy either.
        (await SetPolicyAsync(_b.User, _a.AccountId, true)).StatusCode.Should().NotBe(HttpStatusCode.NoContent);
        (await OwnerScalarAsync("SELECT count(*) FROM account.account_settings")).Should().Be("0");

        (await SetPolicyAsync(_a.User, _a.AccountId, true)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await OwnerScalarAsync("SELECT setting_value FROM account.account_settings WHERE account_id = @a", ("a", _a.AccountId)))
            .Should().Be("true");

        var asAdmin = await JsonAsync(await AsAccountMemberAsync(_accountAdmin, _a.AccountId, HttpMethod.Get, "/api/v1/accounts/security-policy"));
        asAdmin.GetProperty("requireMfa").GetBoolean().Should().BeTrue();
        var asMember = await JsonAsync(await AsAccountMemberAsync(_accountMember, _a.AccountId, HttpMethod.Get, "/api/v1/accounts/security-policy"));
        asMember.GetProperty("requireMfa").GetBoolean().Should().BeTrue();

        (await SetPolicyAsync(_a.User, _a.AccountId, false)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await OwnerScalarAsync("SELECT setting_value FROM account.account_settings WHERE account_id = @a", ("a", _a.AccountId)))
            .Should().Be("false", "the same row is updated, not duplicated");
    }

    [Fact]
    public async Task PolicyOff_SignInIsUnchanged()
    {
        var login = await LoginAsync("a-mfapolicy@example.com");
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        SetsSessionCookies(login).Should().BeTrue();
        (await JsonAsync(login)).GetProperty("mfaEnrollmentRequired").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PolicyOn_NoFactor_SignInIssuesNoTokens_AndAnEnrollmentTokenThatNothingElseAccepts()
    {
        (await SetPolicyAsync(_a.User, _a.AccountId, true)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await LoginAsync("a-mfapolicy@example.com");
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        SetsSessionCookies(login).Should().BeFalse("no session exists until a factor is enrolled");
        var body = await JsonAsync(login);
        body.GetProperty("mfaEnrollmentRequired").GetBoolean().Should().BeTrue();
        body.GetProperty("mfaRequired").GetBoolean().Should().BeFalse();
        var token = body.GetProperty("mfaEnrollmentToken").GetString()!;
        token.Should().NotBeNullOrWhiteSpace();
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u", ("u", _a.User.Id)))
            .Should().Be("0");

        // The token is a Redis record, not a credential: no other endpoint takes it, however it is presented.
        using (var client = Factory.CreateClient())
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            (await client.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.PostAsJsonAsync("/api/v1/auth/mfa/enrollment/start", new { stepUpToken = token }))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await PostAsync("/api/v1/auth/mfa/verify", new { challengeToken = token, code = "000000" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "a login challenge endpoint rejects an enrollment token");
        (await PostAsync("/api/v1/auth/refresh", new { }, cookie: $"refreshToken={token}"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostAsync("/api/v1/auth/refresh", new { }, cookie: $"accessToken={token}"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PostAsync("/api/v1/auth/mfa/required-enrollment/start", new { enrollmentToken = Guid.NewGuid().ToString("N") }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "an unknown token opens nothing");
    }

    [Fact]
    public async Task PolicyOn_FinishingEnrollmentFinishesTheSignIn_AndTheNextSignInIsAnOrdinaryMfaSignIn()
    {
        (await SetPolicyAsync(_a.User, _a.AccountId, true)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await JsonAsync(await LoginAsync("a-mfapolicy@example.com"));
        var token = login.GetProperty("mfaEnrollmentToken").GetString()!;
        var start = await JsonAsync(await PostAsync("/api/v1/auth/mfa/required-enrollment/start", new { enrollmentToken = token }));
        var secret = start.GetProperty("secret").GetString()!;
        var methodId = start.GetProperty("mfaMethodId").GetGuid();

        (await PostAsync("/api/v1/auth/mfa/required-enrollment/complete", new { enrollmentToken = token, mfaMethodId = methodId, code = "000000" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "a wrong code activates nothing and opens no session");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u", ("u", _a.User.Id))).Should().Be("0");

        var done = await PostAsync("/api/v1/auth/mfa/required-enrollment/complete",
            new { enrollmentToken = token, mfaMethodId = methodId, code = TotpCode(secret) });
        done.StatusCode.Should().Be(HttpStatusCode.OK, await done.Content.ReadAsStringAsync());
        SetsSessionCookies(done).Should().BeTrue("the sign-in finishes with the enrollment");
        (await JsonAsync(done)).GetProperty("recoveryCodes").GetArrayLength().Should().BeGreaterThan(0);
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u AND status = 'Active'", ("u", _a.User.Id)))
            .Should().Be("1");

        (await PostAsync("/api/v1/auth/mfa/required-enrollment/complete",
                new { enrollmentToken = token, mfaMethodId = methodId, code = TotpCode(secret) }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest, "the token is single-use");

        // With a factor the person signs in the ordinary way: password, then the code.
        var second = await LoginAsync("a-mfapolicy@example.com");
        SetsSessionCookies(second).Should().BeFalse();
        var challenge = await JsonAsync(second);
        challenge.GetProperty("mfaRequired").GetBoolean().Should().BeTrue();
        challenge.GetProperty("mfaEnrollmentRequired").GetBoolean().Should().BeFalse();
        var verified = await PostAsync("/api/v1/auth/mfa/verify",
            new { challengeToken = challenge.GetProperty("mfaChallengeToken").GetString(), code = TotpCode(secret) });
        verified.StatusCode.Should().Be(HttpStatusCode.OK, await verified.Content.ReadAsStringAsync());
        SetsSessionCookies(verified).Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_IsRefusedForAPersonWithNoFactor_WhenTheirAccountStartsRequiringMfa_AndWorksAgainOnceTheyHaveOne()
    {
        var raw = "refresh-" + Guid.NewGuid().ToString("N");
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            ctx.Sessions.Add(UserSession.Create(_a.User.Id, RefreshTokenHash.Create(raw), Now.AddDays(30), Now));
            await ctx.SaveChangesAsync();
        }

        (await SetPolicyAsync(_a.User, _a.AccountId, true)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refused = await PostAsync("/api/v1/auth/refresh", new { }, cookie: $"refreshToken={raw}");
        refused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        SetsSessionCookies(refused).Should().BeFalse();
        (await refused.Content.ReadAsStringAsync()).Should().Contain("mfa-enrollment-required");

        // Enrolling ends the person's other sessions (the old one included): they sign in again, with a factor.
        await EnrolAsync("a-mfapolicy@example.com");
        (await OwnerScalarAsync("SELECT status FROM identity.user_sessions WHERE user_id = @u ORDER BY created_at LIMIT 1", ("u", _a.User.Id)))
            .Should().NotBe("Active");
    }

    [Fact]
    public async Task APolicyOnAccountA_DoesNotTouchAPersonWhoIsOnlyInAccountB()
    {
        (await SetPolicyAsync(_a.User, _a.AccountId, true)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await LoginAsync("b-mfapolicy@example.com");
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        SetsSessionCookies(login).Should().BeTrue();
        (await JsonAsync(login)).GetProperty("mfaEnrollmentRequired").GetBoolean().Should().BeFalse();

        var raw = "refresh-" + Guid.NewGuid().ToString("N");
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            ctx.Sessions.Add(UserSession.Create(_b.User.Id, RefreshTokenHash.Create(raw), Now.AddDays(30), Now));
            await ctx.SaveChangesAsync();
        }

        (await PostAsync("/api/v1/auth/refresh", new { }, cookie: $"refreshToken={raw}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task APersonInSeveralAccounts_IsHeldToTheMfaRequirementOfAnyOneOfThem()
    {
        // Only Account A requires MFA; the person is also in Account B, which does not.
        (await SetPolicyAsync(_a.User, _a.AccountId, true)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var login = await JsonAsync(await LoginAsync("both-mfapolicy@example.com"));
        login.GetProperty("mfaEnrollmentRequired").GetBoolean().Should().BeTrue(
            "one requiring Account is enough, the stricter reading");

        // Once no Account requires it any more, the same person signs in as before.
        (await SetPolicyAsync(_a.User, _a.AccountId, false)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await LoginAsync("both-mfapolicy@example.com");
        SetsSessionCookies(after).Should().BeTrue();
    }
}
