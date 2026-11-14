using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Notrelix.API.ErrorHandling;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.API.Tests.Contracts;

/// <summary>Chat rate limits and slow mode on the checked-in producer contract and in the problem they answer with (UC-CHT-H08).</summary>
public class ChatRateLimitContractOpenApiTests
{
    private static readonly JsonElement Document = Load();
    private static JsonElement Paths => Document.GetProperty("paths");

    [Theory]
    [InlineData("/api/v1/workspaces/{workspaceId}/chat/spaces/{spaceId}/messages", "post")]
    [InlineData("/api/v1/workspaces/{workspaceId}/chat/messages/{rootId}/replies", "post")]
    [InlineData("/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/reactions/{kind}", "put")]
    [InlineData("/api/v1/workspaces/{workspaceId}/chat/messages/{messageId}/attachments", "post")]
    public void TheLimitedOperations_Document429(string path, string verb)
    {
        var responses = Paths.GetProperty(path).GetProperty(verb).GetProperty("responses");

        responses.TryGetProperty("429", out var tooMany).Should().BeTrue($"{verb} {path} answers 429");
        tooMany.GetProperty("content").TryGetProperty("application/problem+json", out _).Should().BeTrue();
    }

    [Fact]
    public void TheChannelUpdate_TakesAnOptionalSlowMode()
    {
        var schema = Document.GetProperty("components").GetProperty("schemas")
            .GetProperty("Notrelix.API.Contracts.Chat.Requests.UpdateChannelRequest").GetProperty("properties");

        var slowMode = schema.GetProperty("slowModeSeconds");

        slowMode.GetProperty("type").GetString().Should().Be("integer");
        slowMode.GetProperty("nullable").GetBoolean().Should().BeTrue("a request that leaves it out keeps it");
    }

    [Fact]
    public void ALimit_Answers429WithTheStableRuleAndTheWholeSecondsToWait()
    {
        var exception = new TooManyRequestsException("Too many requests.", ChatRuleCodes.RateLimited, TimeSpan.FromSeconds(7.2))
        {
            Extensions = new Dictionary<string, object?> { ["policy"] = "chat.send", ["scope"] = "person" },
        };

        var problem = ProblemDetailsMapper.Map(new DefaultHttpContext(), exception);

        problem.Status.Should().Be(StatusCodes.Status429TooManyRequests);
        problem.Type.Should().Be("https://docs.notrelix.com/problems/rate_limit-exceeded");
        problem.Extensions["errorCode"].Should().Be(ErrorCodes.TooManyRequests);
        problem.Extensions["ruleCode"].Should().Be("rate-limited");
        problem.Extensions["retryAfter"].Should().Be(8);
        problem.Extensions["policy"].Should().Be("chat.send");
        problem.Extensions["scope"].Should().Be("person");
    }

    [Fact]
    public async Task TheHandler_SetsRetryAfterOnTheResponse_AndSlowModeCarriesItsOwnRule()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GlobalExceptionHandler>.Instance,
            Microsoft.Extensions.Options.Options.Create(new Notrelix.Application.Common.Idempotency.IdempotencyOptions()));

        await handler.TryHandleAsync(context, new TooManyRequestsException("Slow mode.", ChatRuleCodes.SlowMode, TimeSpan.FromSeconds(18)), CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        context.Response.Headers.RetryAfter.ToString().Should().Be("18");
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        body.RootElement.GetProperty("ruleCode").GetString().Should().Be("slow-mode");
    }

    private static JsonElement Load([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend.slnx")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the checked-in producer contract must be found");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "openapi", "notrelix.v1.json"))).RootElement.Clone();
    }
}
