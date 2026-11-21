using System.Net.Http.Headers;
using System.Text;
using Notrelix.API.Middleware;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Chat.Calls.Commands.HandleCallMediaWebhook;

namespace Notrelix.API.Endpoints.Chat.Calls;

/// <summary>
/// The media server's webhook (SPEC-CHAT-MEDIA-CALLS section 6). It is the one route of Chat that carries no user and no CSRF token: its
/// only authority is the signature the media server puts over the raw body. The exemption is declared on this route alone
/// (<see cref="SignatureAuthenticatedWebhookAttribute"/>), and a request without a verifiable signature is 401 before the body is read.
/// </summary>
public static class MapCallMediaWebhookEndpoint
{
    /// <summary>LiveKit posts <c>application/webhook+json</c>; plain JSON is accepted too.</summary>
    private static readonly string[] MediaTypes = ["application/webhook+json", "application/json"];

    public static IEndpointRouteBuilder MapCallMediaWebhookRoute(this IEndpointRouteBuilder app)
    {
        app.MapPublicPost("/api/v1/chat/calls/media-webhook", HandleAsync)
            .WithTags("Chat.Calls")
            .WithName("Chat.Calls.MediaWebhook")
            .WithSummary("Receives a signed media server event")
            .WithDescription("Called by the media server only. The Authorization header carries a JWT signed with the media server key whose sha256 claim is the digest of the raw body; anything unsigned, tampered, wrongly keyed or expired is 401 and changes nothing. The event id makes a replay a no-op. participant_left leaves the call, room_finished ends it; other events are accepted and ignored.")
            .AllowAnonymous()
            .WithMetadata(new SignatureAuthenticatedWebhookAttribute())
            .WithMetadata(new RateLimitPolicyAttribute("CallMediaWebhookByIp"))
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return app;
    }

    private static async Task<IResult> HandleAsync(HttpRequest request, ISender sender, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Headers.Authorization.FirstOrDefault())) return Results.Unauthorized();

        var mediaType = MediaTypeHeaderValue.TryParse(request.ContentType, out var parsed) ? parsed.MediaType : null;
        if (!MediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase)) return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

        byte[]? raw;
        try
        {
            raw = await WebhookBoundedBodyReader.ReadAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status408RequestTimeout);
        }

        if (raw is null) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        string body;
        try
        {
            body = new UTF8Encoding(false, true).GetString(raw);
        }
        catch (DecoderFallbackException)
        {
            return Results.Unauthorized(); // a body that is not text cannot carry a valid digest
        }

        var result = await sender.Send(new HandleCallMediaWebhookCommand(body, request.Headers.Authorization.ToString()), cancellationToken);
        return result.Succeeded ? Results.Ok() : Results.Unauthorized();
    }
}
