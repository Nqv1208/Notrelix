using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Notrelix.API.Tests.Contracts;
using Notrelix.Application.Common.Models;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.API.Tests.Idempotency;

/// <summary>
/// API-03 / FZ-IDEM-05: the HTTP idempotency contract end-to-end through the
/// real endpoint pipeline. The Idempotency-Key header is bound by the endpoint
/// filter into the scoped execution context; typed 400/409/503 ProblemDetails and
/// the Idempotency-Replayed response header are asserted.
///
/// These tests build clients from the factory server WITHOUT the default key
/// injection so each header scenario is under explicit control.
/// </summary>
public class IdempotencyEndpointContractTests : IClassFixture<NotrelixApiFactory>
{
    private const string ChecklistRoute = "/api/v1/board-items/00000000-0000-0000-0000-000000000001/checklists";
    private const string ValidKey = "test-idempotency-key-0000000000000001";

    private readonly NotrelixApiFactory _factory;

    public IdempotencyEndpointContractTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// A client with auth but no automatic Idempotency-Key injection, so the
    /// header under test is fully explicit. Additional service overrides (e.g. a
    /// scripted idempotency store) can be layered on top.
    /// </summary>
    private HttpClient CreateBareClient(Action<IServiceCollection>? configure = null)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                MockResourceScope(services);
                configure?.Invoke(services);
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }

    /// <summary>
    /// The checklist route is resource-scoped; the In-Memory host has no rows, so
    /// resolution is stubbed to keep the focus on the idempotency contract.
    /// </summary>
    private static void MockResourceScope(IServiceCollection services)
    {
        services.RemoveAll<IResourceLocator>();
        services.AddScoped<IResourceLocator>(_ =>
        {
            var mock = new Moq.Mock<IResourceLocator>();
            mock.Setup(x => x.LocateAsync(
                    Moq.It.IsAny<ResourceRef>(), Moq.It.IsAny<Guid>(), Moq.It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResourceLocation(
                    ResourceKind.Create("work-management.checklist"),
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Guid.Parse("A0000000-0000-0000-0000-000000000001"),
                    Guid.Parse("A0000000-0000-0000-0000-000000000001")));
            return mock.Object;
        });
    }

    private static void UseStore(IServiceCollection services, Moq.Mock<IIdempotencyStore> mock)
    {
        services.RemoveAll<IIdempotencyStore>();
        services.AddScoped<IIdempotencyStore>(_ => mock.Object);
    }

    private static StringContent ChecklistBody() =>
        new(JsonSerializer.Serialize(new { title = "Checklist" }), System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task MissingKey_Returns400ValidationProblem()
    {
        using var client = CreateBareClient();

        var response = await client.PostAsync(ChecklistRoute, ChecklistBody());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("validation.failed");
        problem.GetProperty("errors").TryGetProperty("Idempotency-Key", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ShortKey_Returns400ValidationProblem()
    {
        using var client = CreateBareClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", "short");

        var response = await client.PostAsync(ChecklistRoute, ChecklistBody());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("validation.failed");
    }

    [Fact]
    public async Task RepeatedKey_Returns400ValidationProblem_StatingExactlyOneAllowed()
    {
        using var client = CreateBareClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, ChecklistRoute)
        {
            Content = ChecklistBody(),
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", new[] { ValidKey, ValidKey });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "a request carrying more than one Idempotency-Key header is ambiguous");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("validation.failed");
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("Idempotency-Key", out var message).Should().BeTrue();
        message[0].GetString().Should().Contain("Exactly one Idempotency-Key header is allowed");
    }

    [Fact]
    public async Task WhitespacePaddedKey_Returns400ValidationProblem()
    {
        using var client = CreateBareClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", " padded-key-value ");

        var response = await client.PostAsync(ChecklistRoute, ChecklistBody());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("validation.failed");
    }

    [Fact]
    public async Task ValidKey_FirstExecution_SucceedsWithoutReplayHeader()
    {
        using var client = CreateBareClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", ValidKey);

        var response = await client.PostAsync(ChecklistRoute, ChecklistBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Contains("Idempotency-Replayed").Should().BeFalse(
            "the first execution is not a replay");
    }

    [Fact]
    public async Task Replay_ReturnsStoredResult_WithReplayHeader()
    {
        var replayedData = Guid.NewGuid();
        var storedResult = Result<Guid>.Success(replayedData);
        var storedJson = JsonSerializer.Serialize(storedResult, IdempotencyJson.Options);

        var store = new Moq.Mock<IIdempotencyStore>();
        store.Setup(x => x.BeginAsync(Moq.It.IsAny<IdempotencyIdentity>(), Moq.It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(
                IdempotencyBeginStatus.Completed,
                storedJson,
                "work-management.checklists.create-checklist.v1"));

        using var client = CreateBareClient(services => UseStore(services, store));
        client.DefaultRequestHeaders.Add("Idempotency-Key", ValidKey);

        var response = await client.PostAsync(ChecklistRoute, ChecklistBody());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.TryGetValues("Idempotency-Replayed", out var replayed).Should().BeTrue();
        replayed!.Should().Equal("true");

        // ToCreatedResult writes Result<T>.Data (the created id) as the body.
        var body = await response.Content.ReadFromJsonAsync<Guid>();
        body.Should().Be(replayedData,
            "a replay returns the stored result, not a fresh execution");
    }

    [Fact]
    public async Task PayloadMismatch_Returns409TypedConflict()
    {
        var store = new Moq.Mock<IIdempotencyStore>();
        store.Setup(x => x.BeginAsync(Moq.It.IsAny<IdempotencyIdentity>(), Moq.It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdempotencyBeginResult(
                IdempotencyBeginStatus.PayloadMismatch, null, null));

        using var client = CreateBareClient(services => UseStore(services, store));
        client.DefaultRequestHeaders.Add("Idempotency-Key", ValidKey);

        var response = await client.PostAsync(ChecklistRoute, ChecklistBody());

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("idempotency_payload_mismatch");
    }

    [Fact]
    public void OpenApi_DeclaresIdempotencyContract_OnMarkedOperationsOnly()
    {
        // FZ-IDEM-05: OpenAPI includes the required header, 409, 503 and the replay
        // response header — only for endpoints marked with WithIdempotencyKey().
        using var factory = _factory.WithWebHostBuilder(_ => { });
        using var scope = factory.Services.CreateScope();
        var provider = (Swashbuckle.AspNetCore.Swagger.ISwaggerProvider)scope.ServiceProvider
            .GetRequiredService(typeof(Swashbuckle.AspNetCore.Swagger.ISwaggerProvider));

        var document = provider.GetSwagger("v1");

        var checklistPath = document.Paths.Keys.Single(p => p.EndsWith("/checklists") && p.Contains("board-items"));
        var createChecklist = document.Paths[checklistPath].Operations[OperationType.Post];
        createChecklist.Parameters
            .Should().Contain(p => p.Name == "Idempotency-Key" && p.In == ParameterLocation.Header && p.Required,
                "marked operations declare the required Idempotency-Key header");
        createChecklist.Responses.Keys.Should().Contain("409").And.Contain("503");

        var successResponse = createChecklist.Responses
            .Single(r => r.Key.StartsWith('2')).Value;
        successResponse.Headers.Keys.Should().Contain("Idempotency-Replayed");

        var getChecklists = document.Paths[checklistPath].Operations[OperationType.Get];
        getChecklists.Parameters.Should().NotContain(p => p.Name == "Idempotency-Key",
            "unmarked operations must not declare the idempotency contract");
        getChecklists.Responses.Keys.Should().NotContain("503");
    }

    [Fact]
    public async Task IncompleteState_Returns503WithRetryAfter()
    {
        var store = new Moq.Mock<IIdempotencyStore>();
        store.Setup(x => x.BeginAsync(Moq.It.IsAny<IdempotencyIdentity>(), Moq.It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdempotencyIncompleteStateException(
                "work-management.checklists.create-checklist.v1"));

        using var client = CreateBareClient(services => UseStore(services, store));
        client.DefaultRequestHeaders.Add("Idempotency-Key", ValidKey);

        var response = await client.PostAsync(ChecklistRoute, ChecklistBody());

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.TryGetValues("Retry-After", out var retryAfter).Should().BeTrue();
        retryAfter!.First().Should().Be("3");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("idempotency_state_incomplete");
    }
}
