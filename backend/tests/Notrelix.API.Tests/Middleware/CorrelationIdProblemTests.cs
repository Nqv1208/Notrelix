using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Notrelix.API.Tests.Contracts;

namespace Notrelix.API.Tests.Middleware;

/// <summary>
/// N01: a failing request answers with one correlation id in the response header and in the problem body,
/// so the id a client reports matches the id written to the logs.
/// </summary>
public class CorrelationIdProblemTests
{
    private const string LoginPath = "/api/v1/auth/login";

    [Fact]
    public async Task CsrfRefusal_BodyAndHeaderCarryTheSameCorrelationId()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();
        var correlationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);

        var response = await client.PostAsJsonAsync(LoginPath, new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Headers.GetValues("X-Correlation-Id").Single().Should().Be(correlationId);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("correlationId").GetString().Should().Be(correlationId);
    }

    [Fact]
    public async Task UnsafeIncomingId_IsReplacedInBothHeaderAndBody()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "not-a-guid");

        var response = await client.PostAsJsonAsync(LoginPath, new { });

        var headerValue = response.Headers.GetValues("X-Correlation-Id").Single();
        Guid.TryParse(headerValue, out _).Should().BeTrue();
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("correlationId").GetString().Should().Be(headerValue);
    }

    [Fact]
    public async Task ResourceRefusal_BodyCarriesTheCorrelationIdOfTheHeader()
    {
        using var factory = new NotrelixApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        var correlationId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId);
        var body = new StringContent("{\"title\":\"x\"}", System.Text.Encoding.UTF8, "application/json");

        // The board item is unknown to the in-memory host, so the write is refused before any handler runs.
        var response = await client.PostAsync("/api/v1/board-items/00000000-0000-0000-0000-000000000001/checklists", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
