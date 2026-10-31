using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Requests;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The credential family (login, refresh, email verification, password reset, MFA completion) under the
/// application role, and the bounds of the credential scope they share: the database derives the one user a
/// presented credential names, the scope sees and changes only that user, an unknown credential enters
/// nothing, and the scope ends with its transaction.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class CredentialScopeTests : IAsyncLifetime
{
    private static readonly string Password = $"Pw-{Guid.NewGuid():N}-9";
    private static readonly string WrongPassword = $"Pw-{Guid.NewGuid():N}-8";
    private static readonly string NewPassword = $"Pw-{Guid.NewGuid():N}-7";
    private readonly AppRoleTestContainer _fixture;
    private string _hash = string.Empty;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public CredentialScopeTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        using (var f = AppRoleHost.CreateFactory(_fixture, _fixture.OwnerConnectionString))
        using (var scope = f.Services.CreateScope())
            _hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);

        _a = await AppRoleTenants.SeedAsync(_fixture, "a-cred@example.com", "Aaa", _hash);
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-cred@example.com", "Bbb", _hash);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpResponseMessage> CallAsync(
        Func<HttpClient, Task<HttpResponseMessage>> call, Action<IServiceProvider>? arrange = null, string? refreshCookie = null)
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        arrange?.Invoke(factory.Services);
        using var client = factory.CreateClient();
        if (refreshCookie is not null)
            client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshCookie}");
        var response = await call(client);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
        return Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
    }

    private async Task<(User User, string RawToken)> SeedUnconfirmedUserWithTokenAsync(string email)
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create(email, "Unconfirmed", _hash, now, hasPasswordCredential: true);
        var issued = new OneTimeTokenService().Generate(TokenPurpose.EmailVerification);
        var token = EmailVerificationToken.Create(
            user.Id, TokenHash.Create(issued.TokenHash), issued.HashVersion, user.NormalizedEmail, now.AddHours(1), now);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Users.Add(user);
        ctx.EmailVerificationTokens.Add(token);
        await ctx.SaveChangesAsync();
        return (user, issued.RawToken);
    }

    // ───────────────────────── HTTP flows ─────────────────────────

    [Fact]
    public async Task Login_UnderTheAppRole_Works_AndAFailureIsIndistinguishableForKnownAndUnknownAddresses()
    {
        var ok = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a-cred@example.com", password = Password }));
        ok.StatusCode.Should().Be(HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u", ("u", _a.User.Id))).Should().Be("1");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u", ("u", _b.User.Id))).Should().Be("0");

        var wrong = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/login", new { email = "a-cred@example.com", password = WrongPassword }));
        var unknown = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/login", new { email = "nobody@example.com", password = WrongPassword }));
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknown.StatusCode.Should().Be(wrong.StatusCode);
        static string Shape(string body) => System.Text.RegularExpressions.Regex.Replace(body, "\"traceId\":\"[^\"]*\"", "");
        Shape(await unknown.Content.ReadAsStringAsync()).Should().Be(Shape(await wrong.Content.ReadAsStringAsync()), "the answer does not reveal whether the address exists");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions")).Should().Be("1", "failed logins create no session");
    }

    [Fact]
    public async Task Refresh_UnderTheAppRole_RotatesTheSession_AndAnUnknownTokenEntersNothing()
    {
        var raw = "refresh-" + Guid.NewGuid().ToString("N");
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            var now = DateTimeOffset.UtcNow;
            ctx.Sessions.Add(UserSession.Create(_a.User.Id, RefreshTokenHash.Create(raw), now.AddDays(30), now));
            await ctx.SaveChangesAsync();
        }

        var ok = await CallAsync(c => c.PostAsync("/api/v1/auth/refresh", null), refreshCookie: raw);
        ok.StatusCode.Should().Be(HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u AND status = 'Active'", ("u", _a.User.Id))).Should().Be("1");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u AND status <> 'Active'", ("u", _a.User.Id))).Should().Be("0", "the session keeps its id and stays active; only its credential is replaced");

        var unknown = await CallAsync(c => c.PostAsync("/api/v1/auth/refresh", null), refreshCookie: "refresh-unknown");
        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        // The replaced credential answers with its successor for a short grace window (two tabs refreshing
        // together, a lost response retried); past it, presenting it again ends the session.
        var replay = await CallAsync(c => c.PostAsync("/api/v1/auth/refresh", null), refreshCookie: raw);
        replay.StatusCode.Should().Be(HttpStatusCode.OK, "a retry inside the grace window is answered, not treated as theft");
    }

    [Fact]
    public async Task ConfirmEmail_UnderTheAppRole_ConfirmsOnlyTheUserTheTokenBelongsTo()
    {
        var (u, raw) = await SeedUnconfirmedUserWithTokenAsync("unconfirmed-cred@example.com");
        var (other, _) = await SeedUnconfirmedUserWithTokenAsync("other-unconfirmed-cred@example.com");

        var bad = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/email-verification/confirm", new { token = "not-a-token" }));
        bad.IsSuccessStatusCode.Should().BeFalse();
        (await OwnerScalarAsync("SELECT email_confirmed FROM identity.users WHERE id = @u", ("u", u.Id))).Should().Be("False");

        var ok = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/email-verification/confirm", new { token = raw }));
        ok.IsSuccessStatusCode.Should().BeTrue(await ok.Content.ReadAsStringAsync());
        (await OwnerScalarAsync("SELECT email_confirmed FROM identity.users WHERE id = @u", ("u", u.Id))).Should().Be("True");
        (await OwnerScalarAsync("SELECT email_confirmed FROM identity.users WHERE id = @u", ("u", other.Id))).Should().Be("False", "the token is bound to its own user");

        var replay = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/email-verification/confirm", new { token = raw }));
        replay.IsSuccessStatusCode.Should().BeFalse("a used token confirms nothing");
    }

    [Fact]
    public async Task ResendVerification_UnderTheAppRole_IssuesATokenForThatUserOnly()
    {
        var (u, _) = await SeedUnconfirmedUserWithTokenAsync("resend-cred@example.com");
        var (other, _) = await SeedUnconfirmedUserWithTokenAsync("resend-other-cred@example.com");

        var response = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/email-verification/resend", new { email = "resend-cred@example.com" }));
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        (await OwnerScalarAsync("SELECT count(*) FROM identity.email_verification_tokens WHERE user_id = @u AND status = 'Active'", ("u", u.Id))).Should().Be("1");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.email_verification_tokens WHERE user_id = @u", ("u", u.Id))).Should().Be("2", "the previous token was replaced");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.email_verification_tokens WHERE user_id = @u", ("u", other.Id))).Should().Be("1");

        var unknown = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/email-verification/resend", new { email = "nobody-cred@example.com" }));
        unknown.StatusCode.Should().Be(response.StatusCode, "an unknown address answers like a known one");
    }

    [Fact]
    public async Task ResetPassword_UnderTheAppRole_ChangesOnlyTheAddressedUser_AndRevokesItsSessions()
    {
        string code = string.Empty;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            var now = DateTimeOffset.UtcNow;
            ctx.Sessions.Add(UserSession.Create(_a.User.Id, RefreshTokenHash.Create("rt-" + Guid.NewGuid().ToString("N")), now.AddDays(30), now));
            await ctx.SaveChangesAsync();
        }

        var before = await OwnerScalarAsync("SELECT password_hash FROM identity.users WHERE id = @u", ("u", _a.User.Id));
        var beforeB = await OwnerScalarAsync("SELECT password_hash FROM identity.users WHERE id = @u", ("u", _b.User.Id));

        var wrongCode = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { email = "a-cred@example.com", code = "000000", newPassword = NewPassword }));
        wrongCode.IsSuccessStatusCode.Should().BeFalse();

        var ok = await CallAsync(
            c => c.PostAsJsonAsync("/api/v1/auth/reset-password",
                new { email = "a-cred@example.com", code, newPassword = NewPassword }),
            arrange: _ => { });
        // The code is generated through the same Redis the host uses.
        using (var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString))
        using (var scope = factory.Services.CreateScope())
            code = await scope.ServiceProvider.GetRequiredService<IOtpService>().GenerateAsync("forgot-password", "a-cred@example.com");
        ok = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { email = "a-cred@example.com", code, newPassword = NewPassword }));
        ok.IsSuccessStatusCode.Should().BeTrue(await ok.Content.ReadAsStringAsync());

        (await OwnerScalarAsync("SELECT password_hash FROM identity.users WHERE id = @u", ("u", _a.User.Id))).Should().NotBe(before);
        (await OwnerScalarAsync("SELECT password_hash FROM identity.users WHERE id = @u", ("u", _b.User.Id))).Should().Be(beforeB, "another user is untouched");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_sessions WHERE user_id = @u AND status = 'Active'", ("u", _a.User.Id))).Should().Be("0");
    }

    [Fact]
    public async Task ForgotPassword_UnderTheAppRole_DoesNotFail_ForKnownOrUnknownAddresses()
    {
        var known = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "a-cred@example.com" }));
        var unknown = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "nobody-cred@example.com" }));
        ((int)known.StatusCode).Should().BeLessThan(500, await known.Content.ReadAsStringAsync());
        ((int)unknown.StatusCode).Should().BeLessThan(500);
    }

    [Fact]
    public async Task ResolveSlug_UnderTheAppRole_AnswersOneActiveWorkspace_AndNothingAcrossAccounts()
    {
        var slugA = await OwnerScalarAsync("SELECT slug FROM workspace.workspaces WHERE id = @w", ("w", _a.WorkspaceId));
        var slugB = await OwnerScalarAsync("SELECT slug FROM workspace.workspaces WHERE id = @w", ("w", _b.WorkspaceId));

        var ok = await CallAsync(c => c.GetAsync($"/api/v1/accounts/{_a.AccountId}/resolve?slug={slugA}"));
        ok.StatusCode.Should().Be(HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
        (await ok.Content.ReadAsStringAsync()).Should().Contain(_a.WorkspaceId.ToString()).And.Contain("Aaa WS");

        (await CallAsync(c => c.GetAsync($"/api/v1/accounts/{_a.AccountId}/resolve?slug={slugB}"))).StatusCode
            .Should().Be(HttpStatusCode.BadRequest, "B's slug is not resolvable through A's account (the endpoint's existing not-found answer)");
        (await CallAsync(c => c.GetAsync($"/api/v1/accounts/{_a.AccountId}/resolve?slug=nope"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ───────────────────────── scope bounds (SQL) ─────────────────────────

    private async Task<(NpgsqlConnection C, NpgsqlTransaction Tx)> BeginAsync()
    {
        var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        return (c, await c.BeginTransactionAsync());
    }

    private static async Task<string> QAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql, params (string, object)[] p)
    {
        await using var cmd = new NpgsqlCommand(sql, c, tx);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
        return Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
    }

    [Fact]
    public async Task CredentialScope_SeesAndChangesOnlyTheUserTheEmailNames()
    {
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;

        (await QAsync(c, tx, "SELECT ops.enter_credential_scope('email', 'a-cred@example.com', 0)")).Should().Be(_a.User.Id.ToString());
        (await QAsync(c, tx, "SELECT ops.current_request_scope()")).Should().Be("credential");
        (await QAsync(c, tx, "SELECT ops.is_worker_scope()")).Should().Be("False");
        (await QAsync(c, tx, "SELECT count(*) FROM identity.users")).Should().Be("1");
        (await QAsync(c, tx, "SELECT count(*) FROM identity.users WHERE id = @u", ("u", _b.User.Id))).Should().Be("0");
        (await QAsync(c, tx, "SELECT count(*) FROM authz.access_grants WHERE user_id = @u", ("u", _b.User.Id))).Should().Be("0");
        (await QAsync(c, tx, "SELECT count(*) FROM workspace.workspaces WHERE id = @w", ("w", _b.WorkspaceId)))
            .Should().Be("0", "the credential scope reaches no tenant row the user holds no grant on");
        (await QAsync(c, tx, "WITH u AS (UPDATE identity.users SET name = 'Hacked' WHERE id = @u RETURNING 1) SELECT count(*) FROM u", ("u", _b.User.Id)))
            .Should().Be("0", "another user's row cannot be updated");
        (await QAsync(c, tx, "WITH u AS (UPDATE identity.users SET name = 'Aaa2' WHERE id = @u RETURNING 1) SELECT count(*) FROM u", ("u", _a.User.Id)))
            .Should().Be("1");
    }

    [Fact]
    public async Task UnknownCredentials_EnterNothing_AndAWrongKindIsRefused()
    {
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;

        (await QAsync(c, tx, "SELECT ops.enter_credential_scope('email', 'nobody@example.com', 0) IS NULL")).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.enter_credential_scope('refresh-token', 'unknown', 0) IS NULL")).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.enter_credential_scope('email-verification', 'unknown', 1) IS NULL")).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.enter_credential_scope('oauth-identity', E'Google\\x1funknown', 0) IS NULL")).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.enter_credential_scope('email', '', 0) IS NULL")).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.current_request_scope()")).Should().Be("app", "nothing was entered");
        (await QAsync(c, tx, "SELECT count(*) FROM identity.users")).Should().Be("0");

        await QAsync(c, tx, "SAVEPOINT s");
        var act = () => QAsync(c, tx, "SELECT ops.enter_credential_scope('anything-else', 'x', 0)");
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("22023");
    }

    [Fact]
    public async Task CredentialScope_EndsWithItsTransaction()
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        await using (var tx = await c.BeginTransactionAsync())
        {
            (await QAsync(c, tx, "SELECT ops.enter_credential_scope('email', 'b-cred@example.com', 0)")).Should().Be(_b.User.Id.ToString());
            (await QAsync(c, tx, "SELECT count(*) FROM identity.users")).Should().Be("1");
            await tx.CommitAsync();
        }

        await using var next = await c.BeginTransactionAsync();
        (await QAsync(c, next, "SELECT ops.current_request_scope()")).Should().Be("app");
        (await QAsync(c, next, "SELECT count(*) FROM identity.users")).Should().Be("0");
    }

    [Fact]
    public async Task CredentialScope_ByRefreshDigestAndOAuthIdentity_BindsThatSessionsOrIdentitysUser()
    {
        var raw = "refresh-" + Guid.NewGuid().ToString("N");
        var digest = RefreshTokenHash.Create(raw).Hash;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            var now = DateTimeOffset.UtcNow;
            ctx.Sessions.Add(UserSession.Create(_b.User.Id, RefreshTokenHash.Create(raw), now.AddDays(30), now));
            await ctx.SaveChangesAsync();
        }

        await OwnerScalarAsync(
            "INSERT INTO identity.oauth_accounts (id, user_id, provider, provider_id, raw_profile) VALUES (gen_random_uuid(), @u, 'Google', 'sub-1', '{}'::jsonb)",
            ("u", _a.User.Id));

        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;
        (await QAsync(c, tx, "SELECT ops.enter_credential_scope('refresh-token', @d, 0)", ("d", digest))).Should().Be(_b.User.Id.ToString());
        (await QAsync(c, tx, "SELECT count(*) FROM identity.user_sessions")).Should().Be("1");
        (await QAsync(c, tx, "SELECT ops.enter_credential_scope('oauth-identity', E'Google\\x1fsub-1', 0)")).Should().Be(_a.User.Id.ToString());
        (await QAsync(c, tx, "SELECT count(*) FROM identity.user_sessions")).Should().Be("0", "the scope was rebound to the identity's user");
        (await QAsync(c, tx, "SELECT ops.oauth_identity_linked('Google', 'sub-1')")).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.oauth_identity_linked('Google', 'sub-2')")).Should().Be("False");
    }

    [Fact]
    public async Task TheCredentialAndProbeFunctions_AreExecutableByTheApplicationRoleOnly()
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        foreach (var signature in new[]
                 {
                     "ops.enter_credential_scope(text,text,integer)", "ops.enter_invitee_scope(uuid)", "ops.enter_registration_scope(uuid)",
                     "ops.upsert_access_grant(uuid,uuid,uuid,text,timestamptz)", "ops.email_registered(text)", "ops.oauth_identity_linked(text,text)",
                 })
        {
            async Task<bool> CanAsync(string role)
            {
                await using var q = new NpgsqlCommand($"SELECT has_function_privilege('{role}', '{signature}', 'EXECUTE')", c);
                return (bool)(await q.ExecuteScalarAsync())!;
            }

            (await CanAsync("notrelix_app")).Should().BeTrue(signature);
            foreach (var other in new[] { "public", "notrelix_auth", "notrelix_worker", "notrelix_support_readonly" })
                (await CanAsync(other)).Should().BeFalse($"{other} must not execute {signature}");
        }
    }

    [Fact]
    public async Task MfaCompletion_WithAServerHeldChallenge_RunsInTheUserScope_AndRecordsTheFailedAttempt()
    {
        Guid? challengeUser = _a.User.Id;
        string token = string.Empty;
        using (var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString))
        using (var scope = factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<Notrelix.Application.Features.Identity.Mfa.Abstractions.IMfaChallengeStore>();
            token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var now = DateTimeOffset.UtcNow;
            await store.StoreAsync(token, new Notrelix.Application.Features.Identity.Mfa.DTOs.MfaChallengePayload(
                Guid.CreateVersion7(), challengeUser.Value,
                Notrelix.Application.Features.Identity.Mfa.DTOs.MfaChallengePurpose.PasswordLogin,
                now, now.AddMinutes(5), null), TimeSpan.FromMinutes(5));
        }

        var response = await CallAsync(c => c.PostAsJsonAsync("/api/v1/auth/mfa/verify", new { challengeToken = token, code = "000000" }));
        ((int)response.StatusCode).Should().BeLessThan(500, await response.Content.ReadAsStringAsync());
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_security_activity WHERE user_id = @u", ("u", _a.User.Id)))
            .Should().Be("1", "the failed sign-in attempt was recorded for the challenge's user");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.user_security_activity WHERE user_id = @u", ("u", _b.User.Id))).Should().Be("0");
    }
}
