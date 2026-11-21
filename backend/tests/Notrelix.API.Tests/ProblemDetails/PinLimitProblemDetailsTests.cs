using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;
using Notrelix.Domain.Workspaces;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.API.Tests.ProblemDetails;

/// <summary>
/// The 51st pin of a member is refused as a business rule (400) that carries the stable <c>ruleCode</c>
/// <c>Workspaces_Navigation_PinLimitReached</c>, so a client shows its own translated message instead of the
/// producer's English text. Another navigation rule carries its own rule code.
/// </summary>
public class PinLimitProblemDetailsTests
{
    private readonly GlobalExceptionHandler _handler = new(
        Mock.Of<ILogger<GlobalExceptionHandler>>(),
        Microsoft.Extensions.Options.Options.Create(new IdempotencyOptions()));

    private async Task<(int Status, JsonElement Body)> MapAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/workspaces/x/pins/board/y";

        await _handler.TryHandleAsync(context, exception, default);

        context.Response.Body.Position = 0;
        return (context.Response.StatusCode, await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body));
    }

    [Fact]
    public async Task ThePinLimit_IsABusinessRule_WithItsStableRuleCode()
    {
        var (status, body) = await MapAsync(new DomainBusinessRuleException(
            WorkspaceRuleCodes.Workspaces_Navigation_PinLimitReached, "A member can pin at most 50 resources in a workspace."));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.GetProperty("errorCode").GetString().Should().Be("business_rule.violation");
        body.GetProperty("ruleCode").GetString().Should().Be("Workspaces_Navigation_PinLimitReached");
    }

    [Fact]
    public async Task AnotherNavigationRule_CarriesItsOwnRuleCode()
    {
        var (_, body) = await MapAsync(new DomainBusinessRuleException(
            WorkspaceRuleCodes.Workspaces_Navigation_UnsupportedResourceKind, "kind"));

        body.GetProperty("ruleCode").GetString().Should().Be("Workspaces_Navigation_UnsupportedResourceKind");
    }
}
