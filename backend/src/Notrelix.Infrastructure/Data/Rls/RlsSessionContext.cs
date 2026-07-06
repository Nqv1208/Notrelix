namespace Notrelix.Infrastructure.Data.Rls;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private readonly IOptions<RlsOptions> _options;
    private readonly ICurrentTenantContext _tenant;

    public RlsSessionContext(
        IOptions<RlsOptions> options,
        ICurrentTenantContext tenant)
    {
        _options = options;
        _tenant = tenant;
    }

    public async Task ApplyAsync(DatabaseFacade database, CancellationToken cancellationToken)
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

        await database.ExecuteSqlInterpolatedAsync($@"
            SELECT set_config('app.current_user_id', {userId}, true);
            SELECT set_config('app.current_account_id', {accountId}, true);
            SELECT set_config('app.current_workspace_id', {workspaceId}, true);
            SELECT set_config('app.request_scope', {scope}, true);
            SELECT set_config('app.correlation_id', {correlationId}, true);
        ", cancellationToken);
    }
}
