using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Extensibility.Commands.CreateAutomationRule;
using Notrelix.Application.Features.Extensibility.Commands.HandleN8nCallback;
using Notrelix.Application.Features.Extensibility.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Extensibility.Queries.GetAutomationExecutions;
using Notrelix.Application.Features.Extensibility.Queries.GetWorkspaceAutomations;

namespace Notrelix.API.Endpoints.Extensibility;

public static class AutomationEndpoints
{
    private const string SignatureHeader = "X-Notrelix-Signature";
    private const string TimestampHeader = "X-Notrelix-Timestamp";

    public static IEndpointRouteBuilder MapAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaceGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/automations")
            .WithTags("Automations")
            .RequireAuthorization()
            .WithOpenApi();

        workspaceGroup.MapGet("/", GetWorkspaceAutomations)
            .WithName("GetWorkspaceAutomations")
            .WithSummary("Get workspace automation rules");

        workspaceGroup.MapPost("/", CreateAutomationRule)
            .WithName("CreateAutomationRule")
            .WithSummary("Create a workspace automation rule");

        var automationGroup = app
            .MapGroup("/api/v1/automations")
            .WithTags("Automations")
            .RequireAuthorization()
            .WithOpenApi();

        automationGroup.MapPatch("/{automationId:guid}/enabled", SetAutomationRuleEnabled)
            .WithName("SetAutomationRuleEnabled")
            .WithSummary("Enable or disable an automation rule");

        automationGroup.MapGet("/{automationId:guid}/executions", GetAutomationExecutions)
            .WithName("GetAutomationExecutions")
            .WithSummary("Get automation execution history");

        var n8nGroup = app
            .MapGroup("/api/v1/integrations/n8n")
            .WithTags("n8n")
            .AllowAnonymous()
            .WithOpenApi();

        n8nGroup.MapPost("/callback", HandleN8nCallback)
            .WithName("HandleN8nCallback")
            .WithSummary("Receive signed n8n execution callback");

        n8nGroup.MapPost("/events", ReceiveN8nEvent)
            .WithName("ReceiveN8nEvent")
            .WithSummary("Receive signed inbound n8n event");

        return app;
    }

    private static async Task<IResult> GetWorkspaceAutomations(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceAutomationsQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateAutomationRule(
        Guid workspaceId,
        CreateAutomationRuleRequest request,
        ISender sender)
    {
        var result = await sender.Send(new CreateAutomationRuleCommand(
            workspaceId,
            request.Name,
            request.TriggerEvent,
            request.ActionType,
            request.Configuration ?? "{}"));

        return result.ToCreatedResult($"/api/v1/automations/{result.Data}");
    }

    private static async Task<IResult> SetAutomationRuleEnabled(
        Guid automationId,
        SetAutomationRuleEnabledRequest request,
        ISender sender)
    {
        var result = await sender.Send(new SetAutomationRuleEnabledCommand(automationId, request.IsEnabled));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> GetAutomationExecutions(
        Guid automationId,
        ISender sender,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetAutomationExecutionsQuery(automationId, page, pageSize));
        return result.ToApiResult();
    }

    private static async Task<IResult> HandleN8nCallback(
        HttpRequest request,
        ISender sender,
        IN8nSignatureService signatureService,
        IOptions<N8nOptions> options)
    {
        var verifiedBody = await ReadAndVerifySignedBody(request, signatureService, options.Value);
        if (!verifiedBody.Succeeded) return verifiedBody.Result!;

        var callback = JsonSerializer.Deserialize<N8nExecutionCallbackRequest>(
            verifiedBody.Body!,
            JsonOptions);

        if (callback is null) return Results.BadRequest(new { error = "Invalid callback payload." });

        var result = await sender.Send(new HandleN8nCallbackCommand(
            callback.ExecutionId,
            callback.Status,
            callback.Response,
            callback.Error));

        return result.ToNoContentResult();
    }

    private static async Task<IResult> ReceiveN8nEvent(
        HttpRequest request,
        IN8nSignatureService signatureService,
        IOptions<N8nOptions> options)
    {
        var verifiedBody = await ReadAndVerifySignedBody(request, signatureService, options.Value);
        if (!verifiedBody.Succeeded) return verifiedBody.Result!;

        return Results.Accepted();
    }

    private static async Task<SignedBodyResult> ReadAndVerifySignedBody(
        HttpRequest request,
        IN8nSignatureService signatureService,
        N8nOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
            return SignedBodyResult.Fail(Results.Problem("N8n webhook secret is not configured.", statusCode: StatusCodes.Status500InternalServerError));

        if (!request.Headers.TryGetValue(SignatureHeader, out var signature) ||
            !request.Headers.TryGetValue(TimestampHeader, out var timestampRaw) ||
            !long.TryParse(timestampRaw, out var timestamp))
        {
            return SignedBodyResult.Fail(Results.Unauthorized());
        }

        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var verified = signatureService.VerifySignature(
            body,
            timestamp,
            signature.ToString(),
            options.WebhookSecret,
            TimeSpan.FromSeconds(options.SignatureToleranceSeconds));

        return verified
            ? SignedBodyResult.Ok(body)
            : SignedBodyResult.Fail(Results.Unauthorized());
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record CreateAutomationRuleRequest(
        string Name,
        string TriggerEvent,
        string ActionType,
        string? Configuration);

    private sealed record SetAutomationRuleEnabledRequest(bool IsEnabled);

    private sealed record N8nExecutionCallbackRequest(
        Guid ExecutionId,
        string Status,
        string? Response,
        string? Error);

    private sealed record SignedBodyResult(bool Succeeded, string? Body, IResult? Result)
    {
        public static SignedBodyResult Ok(string body) => new(true, body, null);
        public static SignedBodyResult Fail(IResult result) => new(false, null, result);
    }
}
