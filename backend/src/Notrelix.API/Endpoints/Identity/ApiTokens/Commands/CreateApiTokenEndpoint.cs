using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;
using Notrelix.Application.Features.Identity.ApiTokens.DTOs;

namespace Notrelix.API.Endpoints.Identity.ApiTokens.Commands;

public static class CreateApiTokenEndpoint
{
    public static IEndpointRouteBuilder MapCreateApiToken(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithName("Identity.ApiTokens.Create")
            .WithSummary("Issue a new API token for a workspace")
            .WithDescription("Requires a single-use step-up proof for the IssueApiToken purpose. The raw secret is returned exactly once in this response.")
            .Produces<CreatedApiTokenDto>(StatusCodes.Status201Created, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        CreateApiTokenRequest request,
        ISender sender)
    {
        var result = await sender.Send(new CreateApiTokenCommand(
            workspaceId,
            request.Name,
            request.ExpiresAt,
            request.StepUpToken));
        if (!result.Succeeded)
        {
            return result.ToCreatedResult();
        }
        return result.ToCreatedResult($"/api/v1/workspaces/{workspaceId}/api-tokens/{result.Data.Id}");
    }
}

public sealed record CreateApiTokenRequest
{
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; init; }
    public string StepUpToken { get; init; } = string.Empty;
}