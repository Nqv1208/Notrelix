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
            .WithDescription("Requires a single-use step-up proof for the IssueApiToken purpose. The token needs an expiry in the future, at most 365 days away (identity.api-tokens.lifetime-exceeded otherwise), and at least one scope from the published list (identity.api-tokens.scopes-required, identity.api-tokens.unknown-scope); scopes cannot be changed afterwards. A call with the token outside its scopes is answered 403 identity.token.scope-missing. The raw secret is returned exactly once in this response; the issue is recorded in the audit log.")
            .Produces<CreatedApiTokenDto>(StatusCodes.Status201Created, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);
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
            request.Scopes,
            request.StepUpToken));
        if (!result.Succeeded)
        {
            return result.ToCreatedResult();
        }

        var created = result.Data
            ?? throw new InvalidOperationException(
                "Successful API token creation must contain response data.");

        return result.ToCreatedResult($"/api/v1/workspaces/{workspaceId}/api-tokens/{created.Id}");
    }
}

public sealed record CreateApiTokenRequest
{
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; init; }
    /// <summary>At least one of work:read, work:write, docs:read, docs:write, chat:read, chat:write, workspace:read; fixed for the life of the token.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];
    public string StepUpToken { get; init; } = string.Empty;
}