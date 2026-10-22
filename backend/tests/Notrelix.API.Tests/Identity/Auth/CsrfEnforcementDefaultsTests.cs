using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.API.Middleware;
using Notrelix.API.Tests.Contracts;
using Notrelix.Infrastructure.Auth.Csrf;

namespace Notrelix.API.Tests.Identity.Auth;

/// <summary>
/// PF-01 / ADR-005: CSRF enforcement is fail-safe. The committed base
/// configuration enforces; only the Development and Testing appsettings turn
/// it off; and startup validation rejects disablement (or an unsafe origin
/// allow-list) everywhere else.
/// </summary>
public class CsrfEnforcementDefaultsTests
{
    private static string ConfigDirectory =>
        Path.GetDirectoryName(typeof(CsrfOptions).Assembly.Location)!;

    private static bool? ReadEnabled(string environment)
    {
        var path = Path.Combine(ConfigDirectory,
            environment.Length == 0 ? "appsettings.json" : $"appsettings.{environment}.json");
        File.Exists(path).Should().BeTrue($"{path} must be copied to the test output");

        var config = new ConfigurationBuilder().AddJsonFile(path, optional: false).Build();
        return config.GetSection("Security:Csrf").GetValue<bool?>("Enabled");
    }

    [Fact]
    public void Options_DefaultToEnforced_WhenNothingIsConfigured()
    {
        new CsrfOptions().Enabled.Should().BeTrue();
    }

    [Fact]
    public void BaseAppsettings_Enforce()
    {
        ReadEnabled("").Should().BeTrue("the committed base configuration must enforce CSRF");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void OnlyDevelopmentAndTestingAppsettings_DisableEnforcement(string environment)
    {
        ReadEnabled(environment).Should().BeFalse(
            "Development/Testing carry the single documented CSRF exception");
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void StagingAndProductionAppsettings_NeverOverrideEnforcement(string environment)
    {
        ReadEnabled(environment).Should().BeNull(
            "Staging/Production must inherit the enforcing base configuration");
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    [InlineData("QA")]
    public void Validator_RejectsDisabledEnforcement_OutsideDevelopmentAndTesting(string environment)
    {
        var result = CreateValidator(environment, "http://localhost:5173")
            .Validate(null, new CsrfOptions { Enabled = false });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Development/Testing");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Validator_AllowsDisabledEnforcement_InDevelopmentAndTesting(string environment)
    {
        CreateValidator(environment)
            .Validate(null, new CsrfOptions { Enabled = false })
            .Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Validator_AcceptsEnforcement_WithExplicitOrigins(string environment)
    {
        CreateValidator(environment, "https://app.example.com", "http://localhost:3000")
            .Validate(null, new CsrfOptions { Enabled = true })
            .Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("*")]
    [InlineData("app.example.com")]
    [InlineData("ftp://app.example.com")]
    public void Validator_RejectsEnforcement_WithUnsafeOrigins(string origin)
    {
        var result = CreateValidator("Production", origin)
            .Validate(null, new CsrfOptions { Enabled = true });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Cors:AllowedOrigins");
    }

    [Fact]
    public void Validator_RejectsEnforcement_WithoutAnyOrigin()
    {
        var result = CreateValidator("Production")
            .Validate(null, new CsrfOptions { Enabled = true });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("at least one origin");
    }

    [Theory]
    [InlineData("Staging", "false")]
    [InlineData("Production", "false")]
    public async Task Startup_Fails_WhenEnforcementIsDisabledOutsideDevelopmentAndTesting(
        string environment, string enabled)
    {
        using var host = BuildHost(environment, enabled, "https://app.example.com");

        var act = () => host.StartAsync();

        var exception = (await act.Should().ThrowAsync<OptionsValidationException>()).Which;
        exception.Failures.Should().ContainSingle(f => f.Contains("Development/Testing"));
    }

    [Fact]
    public async Task Startup_Fails_WhenEnforcedWithWildcardOrigin()
    {
        using var host = BuildHost("Production", enabled: null, "*");

        var act = () => host.StartAsync();

        await act.Should().ThrowAsync<OptionsValidationException>();
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Startup_Succeeds_AndEnforcesByDefault_WhenNoCsrfSettingIsProvided(string environment)
    {
        using var host = BuildHost(environment, enabled: null, "https://app.example.com");

        await host.StartAsync();

        host.Services.GetRequiredService<IOptions<CsrfOptions>>().Value.Enabled.Should().BeTrue();
        await host.StopAsync();
    }

    private static IHost BuildHost(string environment, string? enabled, string origin)
    {
        var values = new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin };
        if (enabled is not null) values["Security:Csrf:Enabled"] = enabled;

        return new HostBuilder()
            .UseEnvironment(environment)
            .ConfigureAppConfiguration(config => config.AddInMemoryCollection(values))
            .ConfigureServices((context, services) => services.AddApiCsrf(context.Configuration))
            .Build();
    }

    [Fact]
    public async Task Host_WhenEnforced_RejectsEveryUnsafeVerbWithoutToken_AndServesSafeEndpoints()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        (await client.GetAsync("/api/v1/auth/csrf")).StatusCode.Should().Be(HttpStatusCode.OK);

        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
        {
            var request = new HttpRequestMessage(method, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new { email = "a@b.c", password = "x" })
            };
            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"{method} without the double-submit pair must be rejected when CSRF is enforced");
        }
    }

    private static CsrfOptionsValidator CreateValidator(string environment, params string[] origins)
    {
        var values = origins
            .Select((origin, index) => new KeyValuePair<string, string?>($"Cors:AllowedOrigins:{index}", origin))
            .ToList();
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(environment);

        return new CsrfOptionsValidator(env.Object, config);
    }
}
