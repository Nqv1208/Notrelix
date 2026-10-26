using System.Text.Json;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Public.Secrets;

namespace Notrelix.Application.Features.Integrations.Connections.Commands.CompleteIntegrationAuthorization;

/// <summary>
/// The provider sent the member back with a code: the backend redeems it, stores the tokens encrypted
/// behind the secret store (never logged, never returned) and records the connection. The signed `state`
/// must name this member, this workspace and this provider. An active connection of the provider is
/// reauthorized instead of duplicated.
/// </summary>
public record CompleteIntegrationAuthorizationCommand(Guid WorkspaceId, string Provider, string Code, string State)
    : ICommand<Result<IntegrationConnectionDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class CompleteIntegrationAuthorizationCommandHandler
    : IRequestHandler<CompleteIntegrationAuthorizationCommand, Result<IntegrationConnectionDto>>
{
    private readonly IIntegrationDbContext _context;
    private readonly IIntegrationSecretStore _secretStore;
    private readonly IIntegrationOAuthClient _client;
    private readonly IIntegrationOAuthStateProtector _stateProtector;
    private readonly IIntegrationProviderSettings _settings;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public CompleteIntegrationAuthorizationCommandHandler(
        IIntegrationDbContext context,
        IIntegrationSecretStore secretStore,
        IIntegrationOAuthClient client,
        IIntegrationOAuthStateProtector stateProtector,
        IIntegrationProviderSettings settings,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _secretStore = secretStore;
        _client = client;
        _stateProtector = stateProtector;
        _settings = settings;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<IntegrationConnectionDto>> Handle(
        CompleteIntegrationAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var actorId = _requestContext.UserId;
        var entry = IntegrationCatalog.Find(request.Provider);
        var state = _stateProtector.Unprotect(request.State);
        if (entry is null
            || entry.Method != IntegrationConnectMethod.OAuth
            || state is null
            || !string.Equals(state.ProviderKey, entry.Key, StringComparison.Ordinal)
            || state.WorkspaceId != request.WorkspaceId
            || state.UserId != actorId)
        {
            return Failure("integrations.authorization.invalid-state", "The authorization request is not valid or has expired.");
        }

        if (!_settings.IsConfigured(entry.Provider))
        {
            return Failure("integrations.provider.not-configured", "The provider is not configured.");
        }

        var tokens = await _client.ExchangeCodeAsync(entry.Provider, request.Code, state.RedirectUri, cancellationToken);
        if (tokens is null)
        {
            return Failure("integrations.authorization.refused", "The provider did not accept the authorization.");
        }

        var now = _clock.UtcNow;
        var accountId = _requestContext.RequireAccountId();
        var tokenSet = JsonSerializer.Serialize(new { accessToken = tokens.AccessToken, refreshToken = tokens.RefreshToken });
        var secretReference = SecretRef.Create(await _secretStore.StoreAsync(tokenSet, cancellationToken));

        var connection = await _context.IntegrationConnections.FirstOrDefaultAsync(c =>
            c.WorkspaceId == request.WorkspaceId
            && c.Provider == entry.Provider
            && c.DeletedAt == null
            && c.Status != IntegrationConnectionStatus.Revoked, cancellationToken);

        string version;
        if (connection is null)
        {
            connection = IntegrationConnection.Create(
                accountId, request.WorkspaceId, entry.Provider, actorId, now, tokens.ProviderAccountId);
            version = "1";
            connection.RotateSecret(version, secretReference, actorId, now);
            _context.IntegrationConnections.Add(connection);
        }
        else
        {
            version = (int.Parse(connection.CurrentSecretVersion ?? "0") + 1).ToString();
            if (connection.Status is IntegrationConnectionStatus.Expired or IntegrationConnectionStatus.Error)
            {
                connection.Reconnect(tokens.ProviderAccountId, expiresAt: null, actorId, now);
            }

            connection.RotateSecret(version, secretReference, actorId, now);
        }

        _context.IntegrationSecretVersions.Add(IntegrationSecretVersion.Create(connection.Id, version, secretReference, now));
        foreach (var scope in tokens.Scopes) connection.AddScope(scope, actorId, now);

        return Result<IntegrationConnectionDto>.Success(IntegrationConnectionMapper.ToDto(connection));
    }

    private static Result<IntegrationConnectionDto> Failure(string code, string message)
        => Result<IntegrationConnectionDto>.Failure(new ApplicationError(code, message, ApplicationErrorType.BusinessRule));
}
