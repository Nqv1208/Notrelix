using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.ErrorHandling;
using Notrelix.API.Extensions;
using Notrelix.API.Middleware;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Integrations.Calendar.Commands.ConnectCalendar;
using Notrelix.Application.Features.Integrations.Calendar.Commands.DisconnectCalendar;
using Notrelix.API.Contracts.Integrations.Calendar.Requests;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

namespace Notrelix.API.Endpoints.Integrations.Calendar;

public static class CalendarEndpoints
{
    private static IResult BoundaryProblem(string errorCode, string title, string detail, int status, HttpRequest request)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://docs.notrelix.com/problems/{errorCode}",
            Title = title,
            Detail = detail,
            Status = status,
            Instance = request.Path,
        };

        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? "unknown";

        return Results.Problem(problem);
    }

    public static IEndpointRouteBuilder AddCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaceGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/calendar")
            .WithTags("Integrations.Calendar")
            .WithOpenApi();

        workspaceGroup.MapResourcePost("/connections", ConnectAsync)
            .WithName("Integrations.Calendar.Connect")
            .WithSummary("Connect a calendar provider to the workspace")
            .Produces<Guid>(StatusCodes.Status201Created);

        var calendarGroup = app
            .MapGroup("/api/v1/calendar-integrations/{integrationId:guid}")
            .WithTags("Integrations.Calendar")
            .WithOpenApi();

        calendarGroup.MapResourceDelete("/", DisconnectAsync)
            .WithName("Integrations.Calendar.Disconnect")
            .WithSummary("Disconnect a calendar integration")
            .Produces(StatusCodes.Status204NoContent);

        var webhookGroup = app
            .MapGroup("/api/v1/integrations/calendar/webhooks/{provider}/{webhookPath}")
            .WithTags("Integrations.Calendar")
            .WithOpenApi();

        webhookGroup.MapPublicPost("/", WebhookAsync)
            .WithName("Integrations.Calendar.HandleWebhook")
            .WithSummary("Verified provider webhook callback intake")
            .AllowAnonymous()
            .WithMetadata(new SignatureAuthenticatedWebhookAttribute())
            .WithMetadata(new RateLimitPolicyAttribute("WebhookIntakeByIp"));

        return app;
    }

    private static async Task<IResult> WebhookAsync(
        string provider, string webhookPath, HttpRequest request, ISender sender, CancellationToken cancellationToken)
    {
        // Raw HTTP boundary (TAC-AI-FLOW-07): content-type allowlist + bounded
        // raw-byte body read happen BEFORE anything is materialized or trusted.
        // The provider contract is application/json; any other media type is
        // rejected at the boundary with 415. The body is read as exact raw
        // bytes (never re-serialized), bounded for known and unknown lengths.
        var contentType = MediaTypeHeaderValue.TryParse(request.ContentType, out var parsed)
            ? parsed.MediaType
            : null;
        if (!string.Equals(contentType, WebhookBoundedBodyReader.WebhookContentType, StringComparison.OrdinalIgnoreCase))
        {
            return BoundaryProblem(
                ErrorCodes.WebhookUnsupportedMediaType,
                "Unsupported Media Type",
                $"Provider webhook callbacks require Content-Type {WebhookBoundedBodyReader.WebhookContentType}.",
                StatusCodes.Status415UnsupportedMediaType,
                request);
        }

        byte[]? rawBytes;
        try
        {
            rawBytes = await WebhookBoundedBodyReader.ReadAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // The bounded-read timeout is a boundary failure: a provider that
            // drips the body slower than the webhook read deadline is rejected
            // with a stable client/protocol error instead of a 500.
            return BoundaryProblem(
                ErrorCodes.WebhookPayloadTooLarge,
                "Request Timeout",
                "Provider webhook callback body was not fully received within the read deadline.",
                StatusCodes.Status408RequestTimeout,
                request);
        }
        if (rawBytes is null)
        {
            return BoundaryProblem(
                ErrorCodes.WebhookPayloadTooLarge,
                "Payload Too Large",
                $"Provider webhook callback body must not exceed {WebhookBoundedBodyReader.MaxWebhookPayloadBytes} bytes.",
                StatusCodes.Status413PayloadTooLarge,
                request);
        }

        if (rawBytes.Length == 0)
        {
            return BoundaryProblem(
                ErrorCodes.WebhookEmptyBody,
                "Bad Request",
                "Provider webhook callback body must not be empty.",
                StatusCodes.Status400BadRequest,
                request);
        }

        // Exact raw bytes are preserved for signature and hash semantics: the
        // string is a strict UTF-8 decode of the same bytes the provider signed.
        string rawBody;
        try
        {
            rawBody = new UTF8Encoding(false, true).GetString(rawBytes);
        }
        catch (DecoderFallbackException)
        {
            return BoundaryProblem(
                ErrorCodes.WebhookMalformedBody,
                "Bad Request",
                "Provider webhook callback body is not valid UTF-8.",
                StatusCodes.Status400BadRequest,
                request);
        }

        try
        {
            using var _ = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return BoundaryProblem(
                ErrorCodes.WebhookMalformedBody,
                "Bad Request",
                "Provider webhook callback body is not valid JSON.",
                StatusCodes.Status400BadRequest,
                request);
        }

        var signature = request.Headers["X-Calendar-Signature"].FirstOrDefault() ?? string.Empty;
        var timestamp = request.Headers["X-Calendar-Timestamp"].FirstOrDefault() ?? string.Empty;

        // The callback travels the canonical request pipeline (validation,
        // descriptors, contracts) — not a direct handler invocation. The
        // route now carries the per-connection webhook path; the legacy
        // provider-only route no longer exists and fails closed (404).
        var result = await sender.Send(
            new HandleCalendarWebhookCommand(provider, signature, timestamp, rawBody, webhookPath),
            cancellationToken);

        return result.Succeeded ? Results.Ok() : Results.Unauthorized();
    }

    private static async Task<IResult> ConnectAsync(Guid workspaceId, ConnectCalendarRequest body, ISender sender)
    {
        var result = await sender.Send(new ConnectCalendarCommand(
            body.Provider, body.AccessToken, workspaceId, body.ProviderAccountId, body.SyncDirection));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> DisconnectAsync(Guid integrationId, ISender sender)
    {
        var result = await sender.Send(new DisconnectCalendarCommand(integrationId));
        return result.ToNoContentResult();
    }
}
