using Microsoft.EntityFrameworkCore.Storage;
namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private readonly DatabaseFacade _database;
    private readonly IOptions<RlsOptions> _options;
    private readonly ICurrentTenantContext _tenant;
    private readonly ILogger<RlsSessionContext>? _logger;

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

    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
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
        ", cancellationToken);
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
            _logger?.LogWarning(
                "Token scope entered: {Purpose} | {Category}: {Description} | account {AccountId} workspace {WorkspaceId}",
                purpose, request.Reason.Category, request.Reason.Description, binding.AccountId, binding.WorkspaceId);
        }

        return binding;
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
