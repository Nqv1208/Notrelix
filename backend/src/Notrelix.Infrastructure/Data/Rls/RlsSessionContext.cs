namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private readonly DatabaseFacade _database;
    private readonly IOptions<RlsOptions> _options;
    private readonly ICurrentTenantContext _tenant;

    public RlsSessionContext(
        ApplicationDbContext dbContext,
        IOptions<RlsOptions> options,
        ICurrentTenantContext tenant)
    {
        _database = dbContext.Database;
        _options = options;
        _tenant = tenant;
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
