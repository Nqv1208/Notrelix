using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notrelix.API.Tests.Contracts;

public class HealthEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;
    private readonly HttpClient _client;

    public HealthEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/health", HealthStatus.Healthy, HttpStatusCode.OK)]
    [InlineData("/health", HealthStatus.Degraded, HttpStatusCode.OK)]
    [InlineData("/health", HealthStatus.Unhealthy, HttpStatusCode.ServiceUnavailable)]
    [InlineData("/health/ready", HealthStatus.Healthy, HttpStatusCode.OK)]
    [InlineData("/health/ready", HealthStatus.Degraded, HttpStatusCode.OK)]
    [InlineData("/health/ready", HealthStatus.Unhealthy, HttpStatusCode.ServiceUnavailable)]
    public async Task Probe_ShouldMapReportStatusToHttpStatus(string path, HealthStatus reported, HttpStatusCode expected)
    {
        using var factory = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddHealthChecks().Add(new HealthCheckRegistration(
                "probe-check",
                _ => new FixedHealthCheck(reported),
                failureStatus: null,
                tags: ["ready"]))));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Test-Roles", "InternalService");

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task GetLive_WhenNotAuthenticated_ShouldReturnAlive()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("alive");
        body.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetLive_WhenDependencyIsUnhealthy_ShouldStillReturnAlive()
    {
        using var factory = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddHealthChecks().Add(new HealthCheckRegistration(
                "broken-dependency",
                _ => new FixedHealthCheck(HealthStatus.Unhealthy),
                failureStatus: null,
                tags: ["ready"]))));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class FixedHealthCheck(HealthStatus status) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new HealthCheckResult(status));
    }
}
