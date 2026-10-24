using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Notrelix.API.Options;
using Notrelix.API.RateLimiting;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.RateLimiting;

public sealed class RateLimitPolicyCatalogTests : IClassFixture<NotrelixApiFactory>
{
    private readonly NotrelixApiFactory _factory;

    public RateLimitPolicyCatalogTests(NotrelixApiFactory factory) => _factory = factory;

    [Fact]
    public void EveryPolicyDeclaredByAnEndpoint_IsConfigured()
    {
        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>();
        var provider = _factory.Services.GetRequiredService<IRateLimitPolicyProvider>();

        var declared = endpoints.Endpoints
            .Select(endpoint => endpoint.Metadata.GetMetadata<RateLimitPolicyAttribute>()?.PolicyName)
            .Where(name => name is not null)
            .ToList();

        declared.Should().NotBeEmpty("auth endpoints declare rate-limit policies");
        RateLimitingOptionsValidator.FindUnconfiguredPolicies(endpoints, provider).Should().BeEmpty();
    }

    [Fact]
    public void BoundOptions_ExposeTheConfiguredPolicies()
    {
        var options = _factory.Services.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        options.Policies.Keys.Should().Contain(["AuthCredentialByIp", "AuthRefreshByIp", "CsrfByIp", "AuthSensitiveByUser"]);
    }

    [Fact]
    public void Catalog_ReportsAnEndpointPolicyThatIsNotConfigured()
    {
        var provider = new RateLimitPolicyProvider(Microsoft.Extensions.Options.Options.Create(new RateLimitingOptions()));
        var endpoints = new TestEndpointDataSource(new RateLimitPolicyAttribute("NoSuchPolicy"));

        RateLimitingOptionsValidator.FindUnconfiguredPolicies(endpoints, provider).Should().Equal("NoSuchPolicy");
    }

    [Fact]
    public void Validator_RejectsNonPositiveLimitsAndUnknownModes()
    {
        var options = new RateLimitingOptions();
        options.Policies["Broken"] = new RateLimitPolicy { PermitLimit = 0, WindowSeconds = 0, Algorithm = "Leaky", FailMode = "Maybe" };

        var result = new RateLimitingOptionsValidator().Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(4);
    }

    [Fact]
    public void Validator_RejectsAnEmptyPolicySet()
    {
        new RateLimitingOptionsValidator().Validate(null, new RateLimitingOptions()).Failed.Should().BeTrue();
    }

    private sealed class TestEndpointDataSource : EndpointDataSource
    {
        private readonly IReadOnlyList<Endpoint> _endpoints;

        public TestEndpointDataSource(RateLimitPolicyAttribute attribute)
        {
            _endpoints =
            [
                new RouteEndpoint(
                    _ => Task.CompletedTask,
                    Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Parse("/probe"),
                    0,
                    new EndpointMetadataCollection(attribute),
                    "probe"),
            ];
        }

        public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

        public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);
    }
}
