using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Notrelix.API.Tests.RateLimiting;

/// <summary>
/// Proves that every shipped rate-limit tier is enforced past its limit, answers with the
/// shared problem shape (dotted error code plus Retry-After) and that the refresh and csrf
/// tiers are not the old five-per-minute bucket. Each test uses one tier only because the
/// in-memory limiter keeps its counters for the lifetime of the shared host.
/// </summary>
public sealed class RateLimitTiersTests : IClassFixture<ShippedRateLimitsApiFactory>
{
    private readonly ShippedRateLimitsApiFactory _factory;

    public RateLimitTiersTests(ShippedRateLimitsApiFactory factory) => _factory = factory;

    private static StringContent Json(string body) => new(body, System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public void ShippedPolicies_DefineTheFourAuthTiers()
    {
        var shipped = new ConfigurationBuilder()
            .AddJsonFile(ShippedRateLimitsApiFactory.ShippedAppSettingsPath, optional: false)
            .Build();

        void Expect(string name, int limit, string failMode, string partition)
        {
            var section = shipped.GetSection($"RateLimiting:Policies:{name}");
            section["PermitLimit"].Should().Be(limit.ToString(), name);
            section["WindowSeconds"].Should().Be("60", name);
            section["FailMode"].Should().Be(failMode, name);
            section["PartitionBy"].Should().Be(partition, name);
        }

        Expect("AuthCredentialByIp", 30, "Closed", "Ip");
        Expect("AuthRefreshByIp", 300, "Open", "Ip");
        Expect("CsrfByIp", 120, "Open", "Ip");
        Expect("AuthSensitiveByUser", 10, "Closed", "UserId");
        shipped.GetSection("RateLimiting:Policies:AuthStrictByIp").Exists().Should().BeFalse();
    }

    [Fact]
    public async Task CredentialTier_IsOneSharedBudgetOfThirty_ThenReturns429WithRetryAfter()
    {
        var client = _factory.CreateClient();
        string[] credentialEndpoints = ["/api/v1/auth/login", "/api/v1/auth/forgot-password", "/api/v1/auth/identify"];

        for (var attempt = 1; attempt <= 30; attempt++)
        {
            var allowed = await client.PostAsync(credentialEndpoints[attempt % credentialEndpoints.Length], Json("{}"));
            allowed.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"attempt {attempt} is within the limit");
        }

        var limited = await client.PostAsync("/api/v1/auth/login", Json("{}"));

        await AssertRateLimitProblemAsync(limited);
    }

    [Fact]
    public async Task RefreshTier_IsNotThrottledAtFivePerMinute_AndLimitsAtThreeHundred()
    {
        var client = _factory.CreateClient();

        for (var attempt = 1; attempt <= 300; attempt++)
        {
            var response = await client.PostAsync("/api/v1/auth/refresh", Json("{}"));
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"refresh attempt {attempt} is within 300 per minute");
        }

        var limited = await client.PostAsync("/api/v1/auth/refresh", Json("{}"));

        await AssertRateLimitProblemAsync(limited);
    }

    [Fact]
    public async Task CsrfTier_AllowsOneHundredTwentyRequestsThenReturns429()
    {
        var client = _factory.CreateClient();

        for (var attempt = 1; attempt <= 120; attempt++)
        {
            var response = await client.GetAsync("/api/v1/auth/csrf");
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"csrf attempt {attempt} is within 120 per minute");
        }

        var limited = await client.GetAsync("/api/v1/auth/csrf");

        await AssertRateLimitProblemAsync(limited);
    }

    [Fact]
    public async Task SensitiveTier_AllowsTenRequestsPerUserThenReturns429()
    {
        var client = _factory.CreateAuthenticatedClient();

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            var response = await client.PostAsync("/api/v1/auth/change-password", Json("{}"));
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"attempt {attempt} is within the limit");
        }

        var limited = await client.PostAsync("/api/v1/auth/change-password", Json("{}"));

        await AssertRateLimitProblemAsync(limited);
    }

    [Fact]
    public async Task SensitiveTier_WithoutASession_AnswersUnauthorizedInsteadOfFailing()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/auth/oauth/google/unlink", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task AssertRateLimitProblemAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
        response.Headers.RetryAfter!.Delta!.Value.TotalSeconds.Should().BeGreaterThan(0);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errorCode").GetString().Should().Be("rate_limit.exceeded");
        body.RootElement.GetProperty("status").GetInt32().Should().Be(429);
    }
}
