using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;

namespace Notrelix.Application.Features.Integrations.Connections.Queries.StartIntegrationAuthorization;

/// <summary>
/// The provider's authorization page for the workspace: the address the member's browser goes to.
/// Nothing is stored; the `state` is a signed, short-lived token naming the member and workspace.
/// A provider without a client id and secret in configuration cannot be connected.
/// </summary>
public record StartIntegrationAuthorizationQuery(Guid WorkspaceId, string Provider, string CallbackBaseUrl)
    : IQuery<Result<IntegrationAuthorizationDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public static class IntegrationCallback
{
    public static string RedirectUri(string callbackBaseUrl, string providerKey)
        => $"{callbackBaseUrl.TrimEnd('/')}/api/v1/integrations/{providerKey}/callback";
}

public class StartIntegrationAuthorizationQueryHandler
    : IRequestHandler<StartIntegrationAuthorizationQuery, Result<IntegrationAuthorizationDto>>
{
    private readonly IIntegrationProviderSettings _settings;
    private readonly IIntegrationOAuthClient _client;
    private readonly IIntegrationOAuthStateProtector _state;
    private readonly ICurrentRequestContext _requestContext;

    public StartIntegrationAuthorizationQueryHandler(
        IIntegrationProviderSettings settings,
        IIntegrationOAuthClient client,
        IIntegrationOAuthStateProtector state,
        ICurrentRequestContext requestContext)
    {
        _settings = settings;
        _client = client;
        _state = state;
        _requestContext = requestContext;
    }

    public Task<Result<IntegrationAuthorizationDto>> Handle(
        StartIntegrationAuthorizationQuery request, CancellationToken cancellationToken)
    {
        var entry = IntegrationCatalog.Find(request.Provider);
        if (entry is null || entry.Method != IntegrationConnectMethod.OAuth)
        {
            return Task.FromResult(Result<IntegrationAuthorizationDto>.Failure(new ApplicationError(
                "integrations.provider.unknown", "The provider cannot be connected this way.", ApplicationErrorType.NotFound)));
        }

        if (!_settings.IsConfigured(entry.Provider))
        {
            return Task.FromResult(Result<IntegrationAuthorizationDto>.Failure(new ApplicationError(
                "integrations.provider.not-configured", "The provider is not configured.", ApplicationErrorType.BusinessRule)));
        }

        var redirectUri = IntegrationCallback.RedirectUri(request.CallbackBaseUrl, entry.Key);
        var state = _state.Protect(new IntegrationOAuthState(entry.Key, request.WorkspaceId, _requestContext.UserId, redirectUri));
        var url = _client.BuildAuthorizeUrl(entry.Provider, redirectUri, state);
        return Task.FromResult(Result<IntegrationAuthorizationDto>.Success(new IntegrationAuthorizationDto(url)));
    }
}
