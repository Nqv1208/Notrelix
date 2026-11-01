using Notrelix.Application.Common.SystemOperations;
using Microsoft.EntityFrameworkCore.Storage;
namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private readonly DatabaseFacade _database;
    private readonly IOptions<RlsOptions> _options;
    private readonly ICurrentTenantContext _tenant;
    private readonly ILogger<RlsSessionContext>? _logger;
    private RlsBoundedScope _active;

    public RlsSessionContext(
        ApplicationDbContext dbContext,
        IOptions<RlsOptions> options,
        ICurrentTenantContext tenant,
        ILogger<RlsSessionContext>? logger = null)
    {
        _database = dbContext.Database;
        _options = options;
        _tenant = tenant;
        _logger = logger;
    }

    public bool IsSessionContextEnabled => _options.Value.SetSessionContext;

    public RlsBoundedScope ActiveBoundedScope => _active;

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        _active = RlsBoundedScope.None;

        if (!_options.Value.SetSessionContext)
        {
            if (!_tenant.IsSystemContext)
            {
                throw new InvalidOperationException(
                    "RLS SetSessionContext is disabled but required for non-system requests. " +
                    "This indicates a misconfiguration. Ensure Rls:SetSessionContext is true in " +
                    "non-development environments.");
            }

            return;
        }

        if (!_tenant.IsSystemContext && !_tenant.AccountId.HasValue)
        {
            throw new InvalidOperationException(
                "AccountId is required for non-system RLS session context. " +
                "Cannot set tenant context without an account identifier.");
        }

        var userId = _tenant.UserId?.ToString() ?? "";
        var accountId = _tenant.AccountId?.ToString() ?? "";
        var workspaceId = _tenant.WorkspaceId?.ToString() ?? "";
        var scope = _tenant.IsSystemContext ? "worker" : "app";
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? "";

        await _database.ExecuteSqlInterpolatedAsync($@"
            SELECT set_config('app.current_user_id', {userId}, true);
            SELECT set_config('app.current_account_id', {accountId}, true);
            SELECT set_config('app.current_workspace_id', {workspaceId}, true);
            SELECT set_config('app.request_scope', {scope}, true);
            SELECT set_config('app.correlation_id', {correlationId}, true);
        ", cancellationToken);
    }

    public async Task ApplyUserScopeAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));

        if (!_options.Value.SetSessionContext)
            return;

        RequireTransaction("user");

        var user = userId.ToString();
        var correlationId = System.Diagnostics.Activity.Current?.Id ?? "";

        // The user scope carries the authenticated user only: account and workspace values are cleared
        // so a policy cannot read a tenant left over from an earlier statement of the same transaction.
        await _database.ExecuteSqlInterpolatedAsync($@"
            SELECT set_config('app.current_user_id', {user}, true);
            SELECT set_config('app.scope_user_id', {user}, true);
            SELECT set_config('app.current_account_id', '', true);
            SELECT set_config('app.current_workspace_id', '', true);
            SELECT set_config('app.request_scope', 'user', true);
            SELECT set_config('app.correlation_id', {correlationId}, true);
            SELECT set_config('app.scope_invitee_id', '', true);
            SELECT set_config('app.scope_token_hash', '', true);
            SELECT set_config('app.scope_token_purpose', '', true);
        ", cancellationToken);
        _active = RlsBoundedScope.User;
    }

    public async Task<TokenScopeBinding?> EnterTokenScopeAsync(
        TokenScopeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.TokenHash))
            throw new ArgumentException("A token digest is required.", nameof(request));

        RequireTransaction("token");

        var purpose = request.Purpose switch
        {
            TokenScopePurpose.WorkspaceInvitation => "workspace-invitation",
            TokenScopePurpose.ApiToken => "api-token",
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Purpose, "Unknown token scope purpose."),
        };

        var connection = (NpgsqlConnection)_database.GetDbConnection();
        var transaction = (NpgsqlTransaction)_database.CurrentTransaction!.GetDbTransaction();
        await using var command = new NpgsqlCommand(
            "SELECT account_id, workspace_id FROM ops.enter_token_scope(@purpose, @hash, @version)",
            connection,
            transaction);
        command.Parameters.AddWithValue("purpose", purpose);
        command.Parameters.AddWithValue("hash", request.TokenHash);
        command.Parameters.AddWithValue("version", request.HashVersion);

        TokenScopeBinding? binding = null;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
                binding = new TokenScopeBinding(reader.GetGuid(0), reader.GetGuid(1));
        }

        RlsScopeMetrics.Record("token", request.Reason.Category, binding is not null);

        if (binding is null)
        {
            _logger?.LogWarning(
                "Token scope not entered: {Purpose} | {Category}: {Description}",
                purpose, request.Reason.Category, request.Reason.Description);
        }
        else
        {
            _active = RlsBoundedScope.Token;
            _logger?.LogInformation(
                "Token scope entered: {Purpose} | {Category}: {Description} | account {AccountId} workspace {WorkspaceId}",
                purpose, request.Reason.Category, request.Reason.Description, binding.AccountId, binding.WorkspaceId);
        }

        return binding;
    }

    public async Task<bool> EnterInviteeScopeAsync(
        Guid userId, SystemOperationReason reason, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(reason);
        RequireTransaction("invitee");

        var bound = await ScalarBoolAsync("SELECT ops.enter_invitee_scope(@user)", "user", userId, cancellationToken);
        RlsScopeMetrics.Record("invitee", reason.Category, bound);
        if (bound)
            _active = RlsBoundedScope.Invitee;
        _logger?.LogInformation(
            bound ? "Invitee scope entered: {Category}: {Description} | user {UserId}"
                  : "Invitee scope not entered: {Category}: {Description} | user {UserId}",
            reason.Category, reason.Description, userId);
        return bound;
    }

    public async Task<bool> EnterRegistrationScopeAsync(
        Guid userId, SystemOperationReason reason, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentNullException.ThrowIfNull(reason);
        RequireTransaction("registration");

        var bound = await ScalarBoolAsync("SELECT ops.enter_registration_scope(@user)", "user", userId, cancellationToken);
        RlsScopeMetrics.Record("registration", reason.Category, bound);
        if (bound)
            _active = RlsBoundedScope.Registration;
        _logger?.LogInformation(
            bound ? "Registration scope entered: {Category}: {Description} | user {UserId}"
                  : "Registration scope not entered: {Category}: {Description} | user {UserId}",
            reason.Category, reason.Description, userId);
        return bound;
    }

    public async Task<Guid?> EnterCredentialScopeAsync(
        CredentialScopeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Credential))
            throw new ArgumentException("A credential is required.", nameof(request));
        RequireTransaction("credential");

        var kind = request.Kind switch
        {
            CredentialScopeKind.NormalizedEmail => "email",
            CredentialScopeKind.RefreshToken => "refresh-token",
            CredentialScopeKind.EmailVerificationToken => "email-verification",
            CredentialScopeKind.OAuthIdentity => "oauth-identity",
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Kind, "Unknown credential scope kind."),
        };

        var connection = (NpgsqlConnection)_database.GetDbConnection();
        var transaction = (NpgsqlTransaction)_database.CurrentTransaction!.GetDbTransaction();
        await using var command = new NpgsqlCommand(
            "SELECT ops.enter_credential_scope(@kind, @credential, @version)", connection, transaction);
        command.Parameters.AddWithValue("kind", kind);
        command.Parameters.AddWithValue("credential", request.Credential);
        command.Parameters.AddWithValue("version", request.HashVersion);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        Guid? userId = result is Guid found ? found : null;

        RlsScopeMetrics.Record("credential", request.Reason.Category, userId is not null);
        if (userId is not null)
            _active = RlsBoundedScope.Credential;

        // The credential itself (an address or a digest) is never logged; only the derived user id is.
        _logger?.LogInformation(
            userId is not null ? "Credential scope entered: {Kind} | {Category}: {Description} | user {UserId}"
                               : "Credential scope not entered: {Kind} | {Category}: {Description}",
            kind, request.Reason.Category, request.Reason.Description, userId);
        return userId;
    }

    /// <summary>
    /// Enters the scope of a verified inbound calendar webhook: bound by the database to the tenant of the
    /// active calendar binding the webhook path identifies. Returns that tenant, or <c>null</c> (no scope
    /// entered) when no active binding has the path. Must run inside the request transaction.
    /// </summary>
    public async Task<TokenScopeBinding?> EnterWebhookScopeAsync(
        string webhookPath, SystemOperationReason reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(webhookPath))
            throw new ArgumentException("Webhook path is required.", nameof(webhookPath));
        ArgumentNullException.ThrowIfNull(reason);
        RequireTransaction("webhook");

        var connection = (NpgsqlConnection)_database.GetDbConnection();
        var transaction = (NpgsqlTransaction)_database.CurrentTransaction!.GetDbTransaction();
        await using var command = new NpgsqlCommand(
            "SELECT account_id, workspace_id FROM ops.enter_webhook_scope(@path)", connection, transaction);
        command.Parameters.AddWithValue("path", webhookPath);

        TokenScopeBinding? binding = null;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
                binding = new TokenScopeBinding(reader.GetGuid(0), reader.GetGuid(1));
        }

        RlsScopeMetrics.Record("webhook", reason.Category, binding is not null);
        if (binding is null)
        {
            _logger?.LogWarning(
                "Webhook scope not entered: {Category}: {Description}", reason.Category, reason.Description);
        }
        else
        {
            _active = RlsBoundedScope.Token;
            // The path is a locator secret and is never logged; only the derived tenant is.
            _logger?.LogInformation(
                "Webhook scope entered: {Category}: {Description} | account {AccountId} workspace {WorkspaceId}",
                reason.Category, reason.Description, binding.AccountId, binding.WorkspaceId);
        }

        return binding;
    }

    private async Task<bool> ScalarBoolAsync(string sql, string parameter, object value, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)_database.GetDbConnection();
        var transaction = (NpgsqlTransaction)_database.CurrentTransaction!.GetDbTransaction();
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(parameter, value);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private void RequireTransaction(string scopeKind)
    {
        if (_database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                $"The {scopeKind} scope is transaction-local and must be entered inside an open transaction.");
        }
    }

    /// <summary>
    /// Applies the narrow anonymous webhook locator context used by the
    /// Calendar binding bootstrap. The RLS policy grants access only to the
    /// row matching this exact route token; it is not a tenant or worker
    /// context and must never be reused as one.
    /// </summary>
    public async Task ApplyWebhookPathAsync(
        string webhookPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(webhookPath))
            throw new ArgumentException("Webhook path is required.", nameof(webhookPath));

        var local = _database.CurrentTransaction is not null;
        await _database.ExecuteSqlInterpolatedAsync($"""
            SELECT set_config('app.webhook_path', {webhookPath}, {local});
            SELECT set_config('app.webhook_connection_id', '', {local});
            """, cancellationToken);
    }

    /// <summary>
    /// Narrows the second bootstrap lookup to the ConnectionId returned by the
    /// already-located CalendarIntegration row.
    /// </summary>
    public async Task ApplyWebhookConnectionAsync(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
            throw new ArgumentException("Connection id is required.", nameof(connectionId));

        var local = _database.CurrentTransaction is not null;
        await _database.ExecuteSqlInterpolatedAsync($"""
            SELECT set_config('app.webhook_connection_id', {connectionId.ToString()}, {local});
            """, cancellationToken);
    }

    /// <summary>
    /// Clears session-level fallback state used by direct/test invocations.
    /// Transaction-local values naturally disappear at commit/rollback; RESET
    /// is still safe inside that transaction and prevents a pooled connection
    /// from retaining the locator when no transaction surrounds the resolver.
    /// </summary>
    public Task ClearWebhookBootstrapAsync(CancellationToken cancellationToken) =>
        _database.ExecuteSqlRawAsync(
            "RESET app.webhook_path; RESET app.webhook_connection_id;",
            cancellationToken);
}
