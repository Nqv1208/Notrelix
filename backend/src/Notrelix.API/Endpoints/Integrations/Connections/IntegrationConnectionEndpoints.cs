using Notrelix.API.Contracts.Integrations.Connections;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Connections;
using Notrelix.Application.Features.Integrations.Connections.Commands.CompleteIntegrationAuthorization;
using Notrelix.Application.Features.Integrations.Connections.Commands.ConnectWebhook;
using Notrelix.Application.Features.Integrations.Connections.Commands.DisconnectIntegration;
using Notrelix.Application.Features.Integrations.Connections.Commands.PauseIntegration;
using Notrelix.Application.Features.Integrations.Connections.Commands.ResumeIntegration;
using Notrelix.Application.Features.Integrations.Connections.Commands.TestIntegration;
using Notrelix.Application.Features.Integrations.Connections.Queries.ListIntegrations;
using Notrelix.Application.Features.Integrations.Connections.Queries.StartIntegrationAuthorization;

namespace Notrelix.API.Endpoints.Integrations.Connections;

/// <summary>The workspace's integrations: catalog, connections, the OAuth redirect flow and the outgoing webhook.</summary>
public static class IntegrationConnectionEndpoints
{
    public static IEndpointRouteBuilder AddIntegrationConnectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/integrations")
            .WithTags("Integrations")
            .WithOpenApi();

        group.MapWorkspaceGet("/", ListAsync)
            .WithName("Integrations.Connections.List")
            .WithSummary("List the integration catalog and the workspace's connections")
            .Produces<IntegrationsOverviewDto>();

        group.MapWorkspaceGet("/{provider}/authorize", AuthorizeAsync)
            .WithName("Integrations.Connections.Authorize")
            .WithSummary("The provider's authorization page for the workspace (OAuth redirect flow)")
            .Produces<IntegrationAuthorizationDto>();

        group.MapWorkspacePost("/webhooks", ConnectWebhookAsync)
            .WithName("Integrations.Webhooks.Connect")
            .WithSummary("Connect an outgoing webhook; the generated signing secret is returned once")
            .Produces<CreatedWebhookConnectionDto>(StatusCodes.Status201Created);

        group.MapWorkspacePost("/{connectionId:guid}/pause", PauseAsync)
            .WithName("Integrations.Connections.Pause")
            .WithSummary("Pause a connection")
            .Produces<IntegrationConnectionDto>();

        group.MapWorkspacePost("/{connectionId:guid}/resume", ResumeAsync)
            .WithName("Integrations.Connections.Resume")
            .WithSummary("Resume a paused connection")
            .Produces<IntegrationConnectionDto>();

        group.MapWorkspacePost("/{connectionId:guid}/test", TestAsync)
            .WithName("Integrations.Connections.Test")
            .WithSummary("Send a test delivery to an outgoing webhook")
            .Produces<IntegrationTestDto>();

        group.MapWorkspaceDelete("/{connectionId:guid}", DisconnectAsync)
            .WithName("Integrations.Connections.Disconnect")
            .WithSummary("Disconnect a connection")
            .Produces(StatusCodes.Status204NoContent);

        app.MapAuthenticatedGet("/api/v1/integrations/{provider}/callback", CallbackAsync)
            .WithTags("Integrations")
            .WithName("Integrations.Connections.Callback")
            .WithSummary("Where the provider sends the member back; redirects to the app with the outcome")
            .Produces(StatusCodes.Status302Found)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> ListAsync(Guid workspaceId, ISender sender)
        => (await sender.Send(new ListIntegrationsQuery(workspaceId))).ToApiResult();

    private static async Task<IResult> AuthorizeAsync(
        Guid workspaceId, string provider, HttpRequest request, IConfiguration configuration, ISender sender)
        => (await sender.Send(new StartIntegrationAuthorizationQuery(
            workspaceId, provider, CallbackBaseUrl(request, configuration)))).ToApiResult();

    private static async Task<IResult> ConnectWebhookAsync(Guid workspaceId, ConnectWebhookRequest body, ISender sender)
    {
        var result = await sender.Send(new ConnectWebhookCommand(workspaceId, body.Url));
        return result.Succeeded
            ? result.ToCreatedResult($"/api/v1/workspaces/{workspaceId}/integrations/{result.Data!.Connection.Id}")
            : result.ToApiResult();
    }

    private static async Task<IResult> PauseAsync(Guid workspaceId, Guid connectionId, ISender sender)
        => (await sender.Send(new PauseIntegrationCommand(workspaceId, connectionId))).ToApiResult();

    private static async Task<IResult> ResumeAsync(Guid workspaceId, Guid connectionId, ISender sender)
        => (await sender.Send(new ResumeIntegrationCommand(workspaceId, connectionId))).ToApiResult();

    private static async Task<IResult> TestAsync(Guid workspaceId, Guid connectionId, ISender sender)
        => (await sender.Send(new TestIntegrationCommand(workspaceId, connectionId))).ToApiResult();

    private static async Task<IResult> DisconnectAsync(Guid workspaceId, Guid connectionId, ISender sender)
        => (await sender.Send(new DisconnectIntegrationCommand(workspaceId, connectionId))).ToNoContentResult();

    private static async Task<IResult> CallbackAsync(
        string provider,
        string? code,
        string? state,
        string? error,
        IConfiguration configuration,
        IIntegrationOAuthStateProtector stateProtector,
        ISender sender)
    {
        var appBase = (configuration["Frontend:AppBaseUrl"] ?? "/").TrimEnd('/');
        var named = string.IsNullOrEmpty(state) ? null : stateProtector.Unprotect(state);
        if (named is null) return Results.Redirect($"{appBase}/");

        var page = $"{appBase}/workspaces/{named.WorkspaceId}/integrations";
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
        {
            return Results.Redirect($"{page}?error=refused");
        }

        var result = await sender.Send(new CompleteIntegrationAuthorizationCommand(named.WorkspaceId, provider, code, state!));
        return Results.Redirect(result.Succeeded
            ? $"{page}?connected={Uri.EscapeDataString(named.ProviderKey)}"
            : $"{page}?error=failed");
    }

    private static string CallbackBaseUrl(HttpRequest request, IConfiguration configuration)
        => configuration["Integrations:PublicApiBaseUrl"] is { Length: > 0 } configured
            ? configured
            : $"{request.Scheme}://{request.Host}";
}
