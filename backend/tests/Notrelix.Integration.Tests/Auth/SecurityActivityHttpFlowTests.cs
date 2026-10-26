using Notrelix.Testing.Core;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notrelix.API;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Auth;

/// <summary>
/// The security activity feed, the lockout defaults, password-reset side effects and the
/// durability of the operator security audit over the real HTTP production graph
/// (PostgreSQL + Redis): NRX-003 (user-scoped read), NRX-012 (no unknown-email storage),
/// and the security audit persistence defect (events were staged after the data session had committed).
/// </summary>
[Collection("Cache")]
[Trait("Category", "Integration")]
public sealed class SecurityActivityHttpFlowTests : IAsyncLifetime
{
    private const string Password = "Correct-Horse-9!";
    private const string AliceEmail = "alice-activity@example.com";
    private const string BobEmail = "bob-activity@example.com";

    private readonly CacheTestContainer _fixture;
    private User _alice = null!;
    private User _bob = null!;

    public SecurityActivityHttpFlowTests(CacheTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();

        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);

        var now = DateTimeOffset.UtcNow;
        _alice = User.Create(AliceEmail, "Alice", hash, now, hasPasswordCredential: true);
        _bob = User.Create(BobEmail, "Bob", hash, now, hasPasswordCredential: true);

        await using var context = CreateSystemContext();
        context.Users.AddRange(_alice, _bob);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ApplicationDbContext CreateSystemContext()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return _fixture.CreatePostgresOwnerContext(tenant);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
            builder.UseEnvironment("Testing");

            foreach (var (rateLimitKey, rateLimitValue) in RateLimitTestSettings.Generous)
            {
                builder.UseSetting(rateLimitKey, rateLimitValue);
            }

            var keysPath = Path.Combine(Path.GetTempPath(), "notrelix-activity-http-keys", Guid.NewGuid().ToString("N"));
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotrelixDb"] = _fixture.PostgresConnectionString,
                ["ConnectionStrings:NotrelixWorkerDb"] = _fixture.PostgresWorkerConnectionString,
                ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
                ["JwtSettings:SecretKey"] = "activity-http-test-signing-key-ThisIsNotASecret-0123456789",
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
                ["Security:Csrf:Enabled"] = "false",
                // The accepted product defaults (10 / 50 per 15 min) are asserted separately; keep the
                // rest of the suite far from the IP limit.
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
            };
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
        => await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    private static async Task<JsonElement> ActivityAsync(HttpClient client, string query = "")
    {
        var response = await client.GetAsync($"/api/v1/auth/security/activity{query}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static string[] Types(JsonElement page) =>
        page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("type").GetString()!).ToArray();

    [Fact]
    public async Task SignInFailureAndSuccess_AreRecordedForTheOwner_NewestFirst_WithSessionId()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        (await LoginAsync(client, AliceEmail, "wrong-password")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await LoginAsync(client, AliceEmail, Password)).StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await ActivityAsync(client);

        Types(page).Should().Equal("SignInSucceeded", "SignInFailed");
        var items = page.GetProperty("items").EnumerateArray().ToArray();
        items[0].GetProperty("sessionId").GetGuid().Should().NotBeEmpty("a sign-in names the session it created");
        items[0].GetProperty("occurredAt").GetDateTimeOffset()
            .Should().BeOnOrAfter(items[1].GetProperty("occurredAt").GetDateTimeOffset());
        page.GetProperty("nextCursor").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task UnknownEmail_LeavesNoActivityRow_AndNeverStoresTheAttemptedEmailOrPassword()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await LoginAsync(client, "nobody-at-all@example.com", "Secret-Attempt-123!");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var context = CreateSystemContext();
        (await context.SecurityActivity.CountAsync()).Should().Be(0);
        var dump = JsonSerializer.Serialize(await context.SecurityActivity.AsNoTracking().ToListAsync());
        dump.Should().NotContain("nobody-at-all").And.NotContain("Secret-Attempt");
    }

    [Fact]
    public async Task Feed_IsLimitedToTheSignedInUser()
    {
        using var factory = CreateFactory();
        using var aliceClient = factory.CreateClient();
        using var bobClient = factory.CreateClient();

        await LoginAsync(aliceClient, AliceEmail, "wrong-password");
        await LoginAsync(aliceClient, AliceEmail, Password);
        await LoginAsync(bobClient, BobEmail, Password);

        var alice = await ActivityAsync(aliceClient);
        var bob = await ActivityAsync(bobClient);

        Types(alice).Should().Equal("SignInSucceeded", "SignInFailed");
        Types(bob).Should().Equal("SignInSucceeded");

        await using var context = CreateSystemContext();
        var bobRows = await context.SecurityActivity.AsNoTracking().Where(e => e.UserId == _bob.Id).Select(e => e.Id).ToListAsync();
        var aliceIds = alice.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid());
        aliceIds.Should().NotIntersectWith(bobRows, "one user can never read another user's activity");
    }

    [Fact]
    public async Task Feed_RequiresAuthentication()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/security/activity");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Feed_PagesWithAKeysetCursor_WithoutGapsOrRepeats()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        for (var i = 0; i < 4; i++)
            await LoginAsync(client, AliceEmail, "wrong-password");
        await LoginAsync(client, AliceEmail, Password);

        var seen = new List<Guid>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var query = $"?limit=2{(cursor is null ? "" : $"&cursor={cursor}")}";
            var page = await ActivityAsync(client, query);
            seen.AddRange(page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()));
            cursor = page.GetProperty("nextCursor").GetString();
            pages++;
        } while (cursor is not null && pages < 10);

        pages.Should().Be(3);
        seen.Should().HaveCount(5).And.OnlyHaveUniqueItems();

        var bad = await client.GetAsync("/api/v1/auth/security/activity?cursor=not-a-cursor");
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var tooBig = await client.GetAsync("/api/v1/auth/security/activity?limit=101");
        tooBig.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SessionRevocation_IsRecordedForSingleAndOtherSessions()
    {
        using var factory = CreateFactory();
        using var current = factory.CreateClient();
        using var second = factory.CreateClient();
        using var third = factory.CreateClient();
        await LoginAsync(current, AliceEmail, Password);
        await LoginAsync(second, AliceEmail, Password);
        await LoginAsync(third, AliceEmail, Password);

        var sessions = JsonDocument.Parse(await current.GetStringAsync("/api/v1/auth/sessions")).RootElement;
        var other = sessions.EnumerateArray().First(s => !s.GetProperty("isCurrent").GetBoolean()).GetProperty("id").GetGuid();

        (await current.PostAsync($"/api/v1/auth/sessions/{other}/revoke", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await current.PostAsync("/api/v1/auth/sessions/revoke-others", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await ActivityAsync(current);
        var types = Types(page);
        types.Should().Contain("SessionRevoked").And.Contain("OtherSessionsRevoked");
        page.GetProperty("items").EnumerateArray()
            .First(i => i.GetProperty("type").GetString() == "SessionRevoked")
            .GetProperty("sessionId").GetGuid().Should().Be(other);
    }

    [Fact]
    public async Task PasswordReset_RecordsActivity_RejectsTheOldAccessToken_AndLiftsTheAccountLock()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        (await LoginAsync(client, AliceEmail, Password)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/v1/auth/security/activity")).StatusCode.Should().Be(HttpStatusCode.OK);

        // 10 wrong passwords is the accepted per-account limit; the 11th attempt is locked even when correct.
        using var attacker = factory.CreateClient();
        for (var i = 0; i < 10; i++)
            (await LoginAsync(attacker, AliceEmail, "wrong-password")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var locked = await LoginAsync(attacker, AliceEmail, Password);
        locked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var lockedBody = await locked.Content.ReadAsStringAsync();
        lockedBody.Should().NotContainEquivalentOf("attemptsLeft").And.NotContainEquivalentOf("remaining");

        await Task.Delay(TimeSpan.FromSeconds(1.2)); // token iat has second precision
        string code;
        using (var scope = factory.Services.CreateScope())
        {
            code = await scope.ServiceProvider.GetRequiredService<IOtpService>()
                .GenerateAsync("forgot-password", AliceEmail);
        }

        const string newPassword = "Brand-New-Passw0rd!";
        var reset = await attacker.PostAsJsonAsync(
            "/api/v1/auth/reset-password", new { email = AliceEmail, code, newPassword });
        reset.StatusCode.Should().Be(HttpStatusCode.OK, await reset.Content.ReadAsStringAsync());

        // Old access token (cookie of the first client) is rejected, like after change-password.
        (await client.GetAsync("/api/v1/auth/security/activity")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The account lock is gone: the new password signs in at once from the same client IP.
        // The user watermark compares at second precision (token iat <= revoked-before is rejected, as
        // after change-password), so a token minted within the same second as the reset is rejected too.
        await Task.Delay(TimeSpan.FromSeconds(1.2));
        using var fresh = factory.CreateClient();
        (await LoginAsync(fresh, AliceEmail, newPassword)).StatusCode.Should().Be(HttpStatusCode.OK);
        Types(await ActivityAsync(fresh)).Should().Contain("PasswordReset");
    }

    [Fact]
    public async Task SecurityAudit_IsDurablyPersisted_ForAFailedSignIn()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        (await LoginAsync(client, AliceEmail, "wrong-password")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var context = CreateSystemContext();
        var events = await context.EnterpriseSecurityEvents.AsNoTracking()
            .Where(e => e.EventType == "LoginFailed").ToListAsync();
        events.Should().ContainSingle("the audit event must be committed, not only staged");
        events[0].Outcome.Should().Be("AuthenticationFailed");
    }

    [Fact]
    public async Task Throttle_ShipsTheAcceptedProductDefaults()
    {
        using var factory = CreateFactory();
        var options = factory.Services.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<Notrelix.Application.Features.Identity.Auth.Throttling.LoginThrottleOptions>>().Value;

        options.MaxFailuresPerAccount.Should().Be(10);
        options.AccountWindow.Should().Be(TimeSpan.FromMinutes(15));
        options.MaxFailuresPerIp.Should().Be(50);
        options.IpWindow.Should().Be(TimeSpan.FromMinutes(15));
        options.LockDuration.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task Retention_DeletesOnlyEntriesOlderThan90Days()
    {
        await using (var seed = CreateSystemContext())
        {
            var now = DateTimeOffset.UtcNow;
            seed.SecurityActivity.AddRange(
                SecurityActivityEntry.Record(_alice.Id, SecurityActivityType.SignInSucceeded, now.AddDays(-91)),
                SecurityActivityEntry.Record(_alice.Id, SecurityActivityType.SignInFailed, now.AddDays(-89)),
                SecurityActivityEntry.Record(_bob.Id, SecurityActivityType.PasswordChanged, now.AddDays(-200)));
            await seed.SaveChangesAsync();
        }

        using var factory = CreateFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var sweeperType = typeof(Notrelix.Infrastructure.BackgroundJobs.SecurityActivityRetentionOptions).Assembly
                .GetType("Notrelix.Infrastructure.BackgroundJobs.SecurityActivityRetentionSweeper")!;
            var sweeper = scope.ServiceProvider.GetRequiredService(sweeperType);
            var deleted = await (Task<int>)sweeperType.GetMethod("RunOnceAsync")!
                .Invoke(sweeper, [CancellationToken.None])!;
            // The hosted retention service starts with the host and may already have swept the same rows.
            deleted.Should().BeLessThanOrEqualTo(2);
        }

        await using var verify = CreateSystemContext();
        var remaining = await verify.SecurityActivity.AsNoTracking().ToListAsync();
        remaining.Should().ContainSingle().Which.Type.Should().Be(SecurityActivityType.SignInFailed);
    }
}
