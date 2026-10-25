using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Public.Secrets;

namespace Notrelix.Application.Features.Integrations.Connections.Commands.ConnectWebhook;

/// <summary>
/// Connects an outgoing webhook: the address deliveries go to. The backend generates the signing secret,
/// stores it encrypted behind the secret store and returns it once; it can never be read again.
/// </summary>
public record ConnectWebhookCommand(Guid WorkspaceId, string Url)
    : ICommand<Result<CreatedWebhookConnectionDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class ConnectWebhookCommandHandler : IRequestHandler<ConnectWebhookCommand, Result<CreatedWebhookConnectionDto>>
{
    public const int MaxWebhooksPerWorkspace = 10;

    private readonly IIntegrationDbContext _context;
    private readonly IIntegrationSecretStore _secretStore;
    private readonly IWebhookTargetPolicy _policy;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;

    public ConnectWebhookCommandHandler(
        IIntegrationDbContext context,
        IIntegrationSecretStore secretStore,
        IWebhookTargetPolicy policy,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock)
    {
        _context = context;
        _secretStore = secretStore;
        _policy = policy;
        _requestContext = requestContext;
        _clock = clock;
    }

    public async Task<Result<CreatedWebhookConnectionDto>> Handle(
        ConnectWebhookCommand request, CancellationToken cancellationToken)
    {
        var refusal = _policy.Validate(request.Url);
        if (refusal is not null)
        {
            return Result<CreatedWebhookConnectionDto>.Failure(new ApplicationError(
                "integrations.webhook.url-refused", refusal, ApplicationErrorType.Validation, "url"));
        }

        var existing = await _context.IntegrationConnections.CountAsync(c =>
            c.WorkspaceId == request.WorkspaceId
            && c.Provider == IntegrationProvider.Webhook
            && c.DeletedAt == null
            && c.Status != IntegrationConnectionStatus.Revoked, cancellationToken);
        if (existing >= MaxWebhooksPerWorkspace)
        {
            return Result<CreatedWebhookConnectionDto>.Failure(new ApplicationError(
                "integrations.webhook.limit-reached",
                $"A workspace has at most {MaxWebhooksPerWorkspace} outgoing webhooks.",
                ApplicationErrorType.BusinessRule));
        }

        var now = _clock.UtcNow;
        var actorId = _requestContext.UserId;
        var accountId = _requestContext.RequireAccountId();

        var signingSecret = WebhookSignature.GenerateSecret();
        var secretReference = SecretRef.Create(await _secretStore.StoreAsync(signingSecret, cancellationToken));

        var connection = IntegrationConnection.Create(
            accountId, request.WorkspaceId, IntegrationProvider.Webhook, actorId, now, targetUrl: request.Url);
        connection.RotateSecret("1", secretReference, actorId, now);
        _context.IntegrationConnections.Add(connection);
        _context.IntegrationSecretVersions.Add(IntegrationSecretVersion.Create(connection.Id, "1", secretReference, now));

        return Result<CreatedWebhookConnectionDto>.Success(
            new CreatedWebhookConnectionDto(IntegrationConnectionMapper.ToDto(connection), signingSecret));
    }
}
