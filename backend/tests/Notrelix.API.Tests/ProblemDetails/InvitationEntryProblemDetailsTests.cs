using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Notrelix.API.ErrorHandling;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Invitations;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.API.Tests.ProblemDetails;

/// <summary>
/// An invitation refused for its initial entries names each refused entry in the additive <c>entries</c> extension
/// of the problem (position, kind, target, reason), with the status and <c>errorCode</c> of any business rule, so the
/// invite form can show the refusal next to the entry. Nothing else on the response changes.
/// </summary>
public class InvitationEntryProblemDetailsTests
{
    private readonly GlobalExceptionHandler _handler = new(
        Mock.Of<ILogger<GlobalExceptionHandler>>(),
        Microsoft.Extensions.Options.Options.Create(new IdempotencyOptions()));

    [Fact]
    public async Task ARefusedEntry_IsListedByPositionKindTargetAndReason()
    {
        var goneTeam = Guid.CreateVersion7();
        var exception = new DomainBusinessRuleException(
            WorkspaceRuleCodes.Workspaces_Invitation_EntryTargetNotActive,
            "Every initial team or space must be an active team or space of this workspace.",
            new Dictionary<string, object?>
            {
                ["entries"] = new List<InvitationEntryRefusal> { new(1, "team", goneTeam, InvitationEntryRefusal.NotActive) },
            });
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/workspaces/x/members";

        await _handler.TryHandleAsync(context, exception, default);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        body.GetProperty("errorCode").GetString().Should().Be("business_rule.violation");
        body.GetProperty("ruleCode").GetString().Should().Be("Workspaces_Invitation_EntryTargetNotActive");
        var entry = body.GetProperty("entries").EnumerateArray().Single();
        entry.GetProperty("index").GetInt32().Should().Be(1);
        entry.GetProperty("kind").GetString().Should().Be("team");
        entry.GetProperty("targetId").GetGuid().Should().Be(goneTeam);
        entry.GetProperty("reason").GetString().Should().Be("not-active");
    }
}
