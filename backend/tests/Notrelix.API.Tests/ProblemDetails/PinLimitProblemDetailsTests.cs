using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;
using Notrelix.Domain.Workspaces;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.API.Tests.ProblemDetails;

/// <summary>
/// The 51st pin of a member is refused with the stable code <c>workspaces.navigation.pin_limit_reached</c> (status
/// unchanged, 400), so a client can show its own translated message instead of the producer's English text.
/// Other Workspaces rules keep the generic business-rule mapping.
/// </summary>
public class PinLimitProblemDetailsTests
{
    private readonly GlobalExceptionHandler _handler = new(
        Mock.Of<ILogger<GlobalExceptionHandler>>(),
        Microsoft.Extensions.Options.Options.Create(new IdempotencyOptions()));

    private async Task<(int Status, string? ErrorCode)> MapAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/workspaces/x/pins/board/y";

        await _handler.TryHandleAsync(context, exception, default);

        context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        return (context.Response.StatusCode, json.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task ThePinLimit_AnswersItsStableCode_WithTheUnchangedStatus()
    {
        var (status, code) = await MapAsync(new DomainBusinessRuleException(
            WorkspaceRuleCodes.Workspaces_Navigation_PinLimitReached, "A member can pin at most 50 resources in a workspace."));

        status.Should().Be(StatusCodes.Status400BadRequest);
        code.Should().Be("workspaces.navigation.pin_limit_reached");
    }

    [Fact]
    public async Task AnotherNavigationRule_KeepsTheGenericBusinessRuleCode()
    {
        var (status, code) = await MapAsync(new DomainBusinessRuleException(
            WorkspaceRuleCodes.Workspaces_Navigation_UnsupportedResourceKind, "kind"));

        status.Should().Be(StatusCodes.Status400BadRequest);
        code.Should().Be("business_rule.violation");
    }
}
