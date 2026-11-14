using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Public.Secrets;

namespace Notrelix.Application.Features.Integrations.Connections.Commands.RegenerateWebhookSecret;

/// <summary>
/// The new signing secret of a webhook connection, shown once. It is the only response that carries it: the
/// secret is never logged, never put in the audit row and never written to an idempotency result store.
/// </summary>
public sealed record RegeneratedWebhookSecretDto(IntegrationConnectionDto Connection, string SigningSecret);

/// <summary>
/// Regenerates the signing secret of an outgoing webhook (UC-INT-E02). The connection must be Active or Paused.
/// The new secret becomes the current version at once, so deliveries signed with the old one are no longer produced;
/// the old version row stays only as history and is never selected again. The caller states the connection version it
/// read (ExpectedVersion): a repeated request with a stale version is refused, so a lost response cannot rotate twice.
/// Permission: ManageIntegrations, as for connecting the webhook (the neighbour that issues the first secret).
/// </summary>
public sealed record RegenerateWebhookSecretCommand(Guid WorkspaceId, Guid ConnectionId, long ExpectedVersion)
    : ICommand<Result<RegeneratedWebhookSecretDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ManageIntegrations;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
    long IExpectedVersionRequest.ExpectedVersion => ExpectedVersion;
    ResourceRef IExpectedVersionRequest.Resource => ResourceRef.Create(ResourceKind.Create("integrations.connection"), ConnectionId, WorkspaceId);
}

public sealed class RegenerateWebhookSecretCommandValidator : AbstractValidator<RegenerateWebhookSecretCommand>
{
    public RegenerateWebhookSecretCommandValidator()
    {
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}

public sealed class RegenerateWebhookSecretCommandHandler
    : IRequestHandler<RegenerateWebhookSecretCommand, Result<RegeneratedWebhookSecretDto>>
{
    /// <summary>The resource kind the expected-version constraint protects (the connection).</summary>
    public const string ConnectionResourceKind = "integrations.connection";

    private readonly IIntegrationDbContext _context;
    private readonly IIntegrationSecretStore _secretStore;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _auditService;

    public RegenerateWebhookSecretCommandHandler(
        IIntegrationDbContext context,
        IIntegrationSecretStore secretStore,
        ICurrentRequestContext requestContext,
        IDateTimeProvider clock,
        IAuditService auditService)
    {
        _context = context;
        _secretStore = secretStore;
        _requestContext = requestContext;
        _clock = clock;
        _auditService = auditService;
    }

    public async Task<Result<RegeneratedWebhookSecretDto>> Handle(
        RegenerateWebhookSecretCommand request, CancellationToken cancellationToken)
    {
        var connection = await IntegrationConnectionLookup.RequireAsync(
            _context, request.WorkspaceId, request.ConnectionId, cancellationToken);

        if (connection.Provider != IntegrationProvider.Webhook
            || connection.Status is not (IntegrationConnectionStatus.Active or IntegrationConnectionStatus.Paused))
        {
            return Result<RegeneratedWebhookSecretDto>.Failure(new ApplicationError(
                "integrations.webhook.secret.connection-state",
                "The signing secret can be regenerated only for an active or paused outgoing webhook.",
                ApplicationErrorType.BusinessRule));
        }

        var now = _clock.UtcNow;
        var actorId = _requestContext.UserId;
        var nextVersion = (NextVersionAfter(connection.CurrentSecretVersion)).ToString(System.Globalization.CultureInfo.InvariantCulture);

        var signingSecret = WebhookSignature.GenerateSecret();
        var secretReference = SecretRef.Create(await _secretStore.StoreAsync(signingSecret, cancellationToken));

        connection.RotateSecret(nextVersion, secretReference, actorId, now);
        _context.IntegrationSecretVersions.Add(IntegrationSecretVersion.Create(connection.Id, nextVersion, secretReference, now));

        // The audit row names the connection and the new version only. Neither the secret nor its reference is in it.
        await _auditService.RecordAsync(
            request.WorkspaceId,
            actorId,
            "RegenerateWebhookSecret",
            ResourceRef.Create(ResourceKind.Create(ConnectionResourceKind), connection.Id, request.WorkspaceId),
            AuditMetadata.Create(),
            AuditSeverity.Info,
            cancellationToken: cancellationToken,
            change: new AuditChange(null, null, null, new { connectionId = connection.Id, secretVersion = nextVersion }));

        return Result<RegeneratedWebhookSecretDto>.Success(new RegeneratedWebhookSecretDto(
            IntegrationConnectionMapper.ToDto(connection),
            signingSecret));
    }

    private static int NextVersionAfter(string? current) =>
        (int.TryParse(current, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : 0) + 1;
}
