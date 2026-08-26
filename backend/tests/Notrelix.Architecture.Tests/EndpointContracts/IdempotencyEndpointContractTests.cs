using System.Reflection;

namespace Notrelix.Architecture.Tests.EndpointContracts;

/// <summary>
/// FZ-IDEM-05 endpoint architecture gates:
/// - every idempotent command construction/send site is under .WithIdempotencyKey();
/// - a marked endpoint really dispatches an idempotent command;
/// - GET/HEAD endpoints are never marked;
/// - the scanner itself is proven by negative/positive inline fixtures.
/// </summary>
public class IdempotencyEndpointContractTests : ArchitectureTestBase
{
    private static readonly Assembly ApplicationAssembly =
        typeof(Notrelix.Application.Common.Behaviors.RequestContractBehavior<,>).Assembly;

    private static HashSet<string> GetIdempotentTypeNames()
    {
        return ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(IIdempotentRequest).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IReadOnlyList<EndpointRegistrationSite> ScanApi()
    {
        var idempotentNames = GetIdempotentTypeNames();
        return GetApiEndpointFiles()
            .SelectMany(file => EndpointIdempotencyScanner.ScanFile(file, idempotentNames))
            .ToList();
    }

    [Fact]
    public void Every_Idempotent_Command_Send_Site_Requires_WithIdempotencyKey()
    {
        var violations = ScanApi()
            .Where(site => site.IdempotentCommands.Count > 0 && !site.HasIdempotencyKeyMarker)
            .Select(site => site.ToString())
            .ToList();

        violations.Should().BeEmpty(
            "every endpoint dispatching an idempotent command must be marked with " +
            ".WithIdempotencyKey() so the HTTP filter binds and validates the Idempotency-Key header");
    }

    [Fact]
    public void Marked_Endpoints_Dispatch_An_Idempotent_Command()
    {
        var violations = ScanApi()
            .Where(site => site.HasIdempotencyKeyMarker && site.IdempotentCommands.Count == 0)
            .Select(site => site.ToString())
            .ToList();

        violations.Should().BeEmpty(
            "WithIdempotencyKey() must only mark endpoints that dispatch an idempotent command");
    }

    [Fact]
    public void Get_Endpoints_Are_Never_Marked()
    {
        var violations = ScanApi()
            .Where(site => site.HttpMethod == "Get" && site.HasIdempotencyKeyMarker)
            .Select(site => site.ToString())
            .ToList();

        violations.Should().BeEmpty(
            "GET endpoints are safe reads and must never require an Idempotency-Key");
    }

    [Fact]
    public void Scanner_Detects_An_Unmarked_Idempotent_Send_Site()
    {
        // Negative fixture: an unmarked endpoint constructing an idempotent command
        // must be flagged by the scanner (proves detection is not vacuous).
        var source = """
            using MediatR;

            namespace Fixture;

            public static class UnmarkedFixtureEndpoint
            {
                public static void Map(IEndpointRouteBuilder group)
                {
                    group.MapPost("/", HandleAsync);
                }

                private static async Task HandleAsync(ISender sender)
                {
                    await sender.Send(new FixtureCommand());
                }
            }
            """;

        var sites = EndpointIdempotencyScanner.ScanSource(source, new HashSet<string> { "FixtureCommand" });

        sites.Should().HaveCount(1, "exactly one endpoint registration exists in the fixture");
        var site = sites.Single();
        site.HttpMethod.Should().Be("Post");
        site.HasIdempotencyKeyMarker.Should().BeFalse();
        site.IdempotentCommands.Should().Equal("FixtureCommand");
    }

    [Fact]
    public void Scanner_Accepts_A_Marked_Idempotent_Send_Site_And_Ignores_Get()
    {
        var source = """
            using MediatR;

            namespace Fixture;

            public static class MarkedFixtureEndpoint
            {
                public static void Map(IEndpointRouteBuilder group)
                {
                    group.MapPost("/", HandleAsync)
                        .WithIdempotencyKey();

                    group.MapGet("/", ReadAsync);
                }

                private static async Task HandleAsync(ISender sender)
                {
                    await sender.Send(new FixtureCommand());
                }

                private static async Task ReadAsync(ISender sender)
                {
                    await sender.Send(new FixtureQuery());
                }
            }
            """;

        var idempotent = new HashSet<string> { "FixtureCommand" };
        var sites = EndpointIdempotencyScanner.ScanSource(source, idempotent);

        var post = sites.Single(s => s.HttpMethod == "Post");
        post.HasIdempotencyKeyMarker.Should().BeTrue();
        post.IdempotentCommands.Should().Equal("FixtureCommand");

        var get = sites.Single(s => s.HttpMethod == "Get");
        get.HasIdempotencyKeyMarker.Should().BeFalse();
        get.IdempotentCommands.Should().BeEmpty(
            "FixtureQuery is not idempotent, so the GET site reports no idempotent commands");
    }
}
